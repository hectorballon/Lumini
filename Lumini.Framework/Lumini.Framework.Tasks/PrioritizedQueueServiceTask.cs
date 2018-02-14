using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using Lumini.Framework.Common;

namespace Lumini.Framework.Tasks
{
    public sealed class PrioritizedQueueServiceTask<T> : BaseServiceTask
        where T : IThreadable
    {
        public delegate void ConsumeItemHandler(T item);

        private readonly ActionBlock<T> _consumerBlock;
        private readonly int _idleTimeInSeconds;
        private readonly Dictionary<int, TaskEventWaitHandle> _loopWaitHandleList;
        private readonly IPropagatorBlock<T, T> _propagationBlock;

        public PrioritizedQueueServiceTask(string name, int idleTimeinSeconds = 10) : base(name)
        {
            _idleTimeInSeconds = idleTimeinSeconds;
            _loopWaitHandleList = new Dictionary<int, TaskEventWaitHandle>();
            _consumerBlock = new ActionBlock<T>(item => { OnItemAvailable?.Invoke(item); });
            _propagationBlock = CreatePropagationBlock();
            _propagationBlock.LinkTo(_consumerBlock, new DataflowLinkOptions {PropagateCompletion = true});
        }

        public Func<T> GetNextItem { get; set; }
        public event ConsumeItemHandler OnItemAvailable;

        public PrioritizedQueue<T> AddPrioritizedQueue(string name, ushort priority = ushort.MaxValue)
        {
            return new PrioritizedQueue<T>(name, this, priority);
        }

        internal void AddResetEvent(TaskEventWaitHandle resetEvent)
        {
            _loopWaitHandleList.Add(_loopWaitHandleList.Count, resetEvent);
        }

        protected override async Task StartTask(CancellationToken token)
        {
            await DoWork(token);
        }

        protected override async Task DoWork(CancellationToken token)
        {
            token.Register(async () => await Stop());
            while (true)
            {
                Status = TaskStatus.Idle;
                var waitHandleArray = GetLocalCopyOfAvailableWaitHandleList(token);
                var eventThatSignaledIndex =
                    WaitHandle.WaitAny(waitHandleArray,
                        new TimeSpan(0, 0, _idleTimeInSeconds));

                if (token.IsCancellationRequested)
                {
                    await WaitForCompletion();
                    token.ThrowIfCancellationRequested();
                }
                if (eventThatSignaledIndex == waitHandleArray.Length - 1) continue;
                if (eventThatSignaledIndex == WaitHandle.WaitTimeout)
                    await Produce(token, ServiceTaskWaitEventSource.Regular, null);
                else
                    await Produce(token, ServiceTaskWaitEventSource.Prioritized,
                        (TaskEventWaitHandle) waitHandleArray[eventThatSignaledIndex]);
            }
        }

        private async Task WaitForCompletion()
        {
            _propagationBlock.Complete();
            await _consumerBlock.Completion;
            if (OnTaskCompleted != null) await OnTaskCompleted(this, null);
        }

        public event TaskEventHandler OnTaskCompleted;

        private WaitHandle[] GetLocalCopyOfAvailableWaitHandleList(CancellationToken token)
        {
            var localCopy = new List<WaitHandle>();
            localCopy.AddRange(_loopWaitHandleList.OrderBy(i => i.Value.Priority).ThenBy(i => i.Key)
                .Select(t => t.Value));
            localCopy.Add(token.WaitHandle);
            var waitHandleArray = new WaitHandle[localCopy.Count];
            localCopy.CopyTo(waitHandleArray);
            return waitHandleArray;
        }

        private async Task Produce(CancellationToken token, ServiceTaskWaitEventSource eventSource,
            TaskEventWaitHandle waitHandle)
        {
            Status = TaskStatus.Running;
            var item = eventSource == ServiceTaskWaitEventSource.Prioritized
                ? ((PrioritizedQueue<T>) waitHandle.Queue).Dequeue()
                : GetNextItem();
            if (item != null)
                await _propagationBlock.SendAsync(item, token);
        }

        private static IPropagatorBlock<T, T> CreatePropagationBlock()
        {
            var delay = TimeSpan.FromMilliseconds(1000);
            var lastItem = DateTime.MinValue;
            return new TransformBlock<T, T>(
                async x =>
                {
                    var waitTime = lastItem + delay - DateTime.UtcNow;
                    if (waitTime > TimeSpan.Zero)
                        await Task.Delay(waitTime);

                    lastItem = DateTime.UtcNow;

                    return x;
                },
                new ExecutionDataflowBlockOptions {MaxDegreeOfParallelism = 1});
        }
    }
}