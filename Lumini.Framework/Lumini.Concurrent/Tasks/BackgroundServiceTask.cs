using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using Lumini.Concurrent.Collections;
using Lumini.Concurrent.Enums;
using Lumini.Concurrent.Helpers;
using TaskStatus = Lumini.Concurrent.Enums.TaskStatus;

namespace Lumini.Concurrent.Tasks
{
    public class BackgroundServiceTask<T> : BaseServiceTask
        where T : class
    {
        public delegate void ItemAvailableDelegate(T item);

        private ActionBlock<T> _consumerBlock;
        private IPropagatorBlock<T, T> _propagationBlock;

        private readonly Dictionary<int, TaskEventWaitHandle> _itemAvailableSignalList;

        public BackgroundServiceTask(IServiceConfiguration configuration)
            : base(configuration)
        {
            _itemAvailableSignalList = new Dictionary<int, TaskEventWaitHandle>();
        }

        public Func<T> GetNextItem { get; set; }
        public event ItemAvailableDelegate OnItemAvailable;

        public PrioritizedQueue<T> AddPrioritizedQueue(string name, ushort priority = ushort.MaxValue)
        {
            return new PrioritizedQueue<T>(name, this, priority);
        }

        internal void AddResetEvent(TaskEventWaitHandle resetEvent)
        {
            _itemAvailableSignalList.Add(_itemAvailableSignalList.Count, resetEvent);
        }

        protected override async Task StartTask(CancellationToken token)
        {
            _consumerBlock = new ActionBlock<T>(item => { OnItemAvailable?.Invoke(item); });
            _propagationBlock = CreatePropagationBlock();
            _propagationBlock.LinkTo(_consumerBlock, new DataflowLinkOptions { PropagateCompletion = true });
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
                        new TimeSpan(0, 0, Settings.IdleTimeInMilliseconds));

                if (token.IsCancellationRequested)
                {
                    await WaitForCompletion();
                    token.ThrowIfCancellationRequested();
                    break;
                }
                if (eventThatSignaledIndex == waitHandleArray.Length - 1) continue;
                if (eventThatSignaledIndex == WaitHandle.WaitTimeout)
                    await Produce(token, ServiceTaskWaitEventSource.Regular, null);
                else
                    await Produce(token, ServiceTaskWaitEventSource.Prioritized,
                        (TaskEventWaitHandle)waitHandleArray[eventThatSignaledIndex]);
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
            localCopy.AddRange(_itemAvailableSignalList.OrderBy(i => i.Value.Priority).ThenBy(i => i.Key)
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
                ? ((PrioritizedQueue<T>)waitHandle.Queue).Dequeue()
                : GetNextItem();
            if (item != null)
                await _propagationBlock.SendAsync(item, token);
        }

        protected virtual IPropagatorBlock<T, T> CreatePropagationBlock()
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
                new ExecutionDataflowBlockOptions { MaxDegreeOfParallelism = 1 });
        }
    }
}