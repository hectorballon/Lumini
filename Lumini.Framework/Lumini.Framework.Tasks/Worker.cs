using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;

namespace Lumini.Framework.Tasks
{
    public class Worker<T> : IWorker
        where T : class
    {
        private readonly BlockingCollection<T> _blockingQueue;
        private readonly BufferBlock<T> _buffer;
        private readonly Task _feederTask;
        private readonly Task _processorTask;
        private readonly ManualResetEvent _semaphore;
        private readonly WorkerStats _stats;

        private DateTime? _lastTimeAssigned;

        public DoWorkDelegate DoWork;

        public Worker(int id, int boundedCapacity, CancellationToken token)
        {
            WorkerId = id;
            _semaphore = new ManualResetEvent(true);
            _stats = new WorkerStats();
            _buffer = new BufferBlock<T>(
                new DataflowBlockOptions
                {
                    CancellationToken = token,
                    BoundedCapacity = boundedCapacity
                });
            _blockingQueue = new BlockingCollection<T>(new ConcurrentQueue<T>());

            _feederTask = Task.Factory.StartNew(() =>
            {
                while (true)
                {
                    if (token.IsCancellationRequested) break;
                    if (!_buffer.TryReceive(out var item))
                    {
                        Thread.Sleep(2000);
                        continue;
                    }
                    _blockingQueue.Add(item);
                    _stats.IncrementItemsInQueue();
                }

                _blockingQueue.CompleteAdding();
            }, token);

            _processorTask = Task.Factory.StartNew(async () =>
            {
                while (!_blockingQueue.IsCompleted)
                {
                    if (!_blockingQueue.TryTake(out var item, 100)) Thread.Sleep(2000);
                    if (item != null)
                        await Do(item)
                            .ContinueWith(task =>
                                {
                                    _stats.DecrementItemsInQueue();
                                    _semaphore.Set();
                                }
                            );
                }
            }, token);
        }

        public bool CanReceiveItems()
        {
            return _blockingQueue.Count < _blockingQueue.BoundedCapacity || _blockingQueue.BoundedCapacity == -1;
        }

        public async Task ReceiveAsync(object item)
        {
            _lastTimeAssigned = DateTime.UtcNow;
            await _buffer.SendAsync((T)item);
            if (!CanReceiveItems()) _semaphore.WaitOne();
        }

        public void WaitForCompletion()
        {
            Task.WaitAll(_feederTask, _processorTask);
        }

        public int WorkerId { get; }

        public WorkerStats GetStats()
        {
            return _stats;
        }

        public DateTime? GetLastTimeAssigned()
        {
            return _lastTimeAssigned;
        }

        private async Task<bool> Do(T item)
        {
            if (DoWork == null) throw new ArgumentNullException(nameof(DoWork));
            var watch = Stopwatch.StartNew();
            var result = await DoWork(item);
            watch.Stop();
            UpdateStats(result, watch);
            return result;
        }

        private void UpdateStats(bool result, Stopwatch watch)
        {
            if (result)
                _stats.IncrementItemsSucceeded();
            else
                _stats.IncrementItemsWithError();
            var elapsedMs = watch.ElapsedMilliseconds;
            _stats.ActivityDurations.TotalTimeInMs += elapsedMs;
            if (_stats.ActivityDurations.MinTimeInMs > elapsedMs) _stats.ActivityDurations.MinTimeInMs = elapsedMs;
            if (_stats.ActivityDurations.MaxTimeInMs < elapsedMs) _stats.ActivityDurations.MaxTimeInMs = elapsedMs;
            _stats.ActivityDurations.AverageTimeInMs = _stats.ActivityDurations.TotalTimeInMs / _stats.ItemsProcessed;
        }
    }
}