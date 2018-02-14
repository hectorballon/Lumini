using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using Lumini.Framework.Common;

namespace Lumini.Framework.Dataflow
{
    public class QueueDataSource<T> : WaitHandle, IDataSource<T>
        where T : class
    {
        private readonly CancellationTokenSource _cts;

        private readonly ManualResetEvent _semaphore;

        private readonly ManualResetEvent _dataLoadSemaphore;
        private readonly DataAvailableResetEvent _signal;

        private bool _disposing;
        private Task _feederTask;
        private AutoResetEvent _fetchEvent;
        private Task _fetchTask;
        private Func<int, IQueryable<T>> _getDataFunc;
        private TransformBlock<T, T> _propagationBlock;
        private bool _propagatorIsBusy;

        protected internal QueueDataSource(string dataSourceName, IDataProvider<T> dataProvider, ushort priority)
        {
            Name = dataSourceName;
            Priority = priority;
            _signal = new DataAvailableResetEvent(false, dataSourceName, this);
            SafeWaitHandle = _signal.SafeWaitHandle;
            dataProvider.AddResetEvent(_signal);
            _fetchEvent = new AutoResetEvent(false);
            _semaphore = new ManualResetEvent(false);
            _cts = new CancellationTokenSource();
            _dataLoadSemaphore = new ManualResetEvent(true);
        }

        protected virtual IProducerConsumerCollection<T> InternalBuffer { get; private set; }

        protected int BufferCount =>
            _propagationBlock.InputCount + _propagationBlock.OutputCount + InternalBuffer.Count;

        public string Name { get; }
        public int BufferSize { get; private set; } = 200;
        public int IdealBufferSize { get; private set; } = 250;
        public float FetchThreasholdRatio { get; private set; } = 0.8f;

        public bool HasItemsInQueue =>
            BufferCount > 0 || _propagatorIsBusy;

        public ushort Priority { get; }

        public virtual async Task<T> GetNextItemAsync()
        {
            if (_getDataFunc == null)
                throw new ArgumentNullException($"{nameof(GetDataFrom)} hasn't been called");

            EnsureFetchTaskIsRunning();

            if (InternalBuffer == null)
                throw new DataflowException("You must call StartInput first. The buffer has not been initialized");

            var fetchCalled = TryFetchData();

            if (!HasItemsInQueue) return null;

            //if (fetchCalled) _dataLoadSemaphore.WaitOne();
            var item = await _propagationBlock.ReceiveAsync();
            if (ItemAvailableforProcessing != null)
                await ItemAvailableforProcessing.Invoke(this, new DataSourceEventArgs<object> { Items = new[] { item } });
            _signal.Set();
            return item;
        }

        public void GetDataFrom(Func<int, IQueryable<T>> method)
        {
            _getDataFunc = method ?? throw new ArgumentNullException($"{nameof(method)} cannot be null");
        }

        public void Reset()
        {
            _signal.Reset();
        }

        public void Stop()
        {
            if (InternalBuffer == null)
                throw new DataflowException("You must call StartInput first. There's nothing to stop now");

            WaitForCompletion().Wait();
        }

        public void Start()
        {
            if (_propagationBlock != null && !_propagationBlock.Completion.IsCompleted)
                throw new DataflowException("You must call StopInput first, as the previous run is still pending");

            InternalBuffer = CreateBuffer();
            _propagationBlock = CreatePropagationBlock();
            _feederTask = Task.Run(async () =>
            {
                while (true)
                {
                    if (!InternalBuffer.TryTake(out var item))
                    {
                        Thread.Sleep(2000);
                        continue;
                    }
                    _semaphore.Set();
                    await _propagationBlock.SendAsync(item);
                }
            }, _cts.Token);
            //InternalBuffer.LinkTo(_propagationBlock, new DataflowLinkOptions { PropagateCompletion = true });
            EnsureFetchTaskIsRunning();
        }

        public async Task WaitForCompletion()
        {
            _cts.Cancel();
            _propagationBlock.Complete();
            await _propagationBlock.Completion;
        }

        public event DataSourceEventHandler DataFound;
        public event DataSourceEventHandler ItemAvailableforProcessing;

        public new virtual void Dispose()
        {
            _disposing = true;
            _fetchEvent.Set();
            _fetchTask.Wait(3000);

            try
            {
                _fetchTask.Dispose();
                _fetchTask = null;
                _fetchEvent.Dispose();
                _fetchEvent = null;
            }
            catch
            {
                // ignored
            }

            base.Dispose();
        }

        private bool TryFetchData()
        {
            var currentRatio = BufferCount * 1.0f / IdealBufferSize;
            if (!(currentRatio <= FetchThreasholdRatio)) return false;
            _fetchEvent.Set();
            return true;
        }

        protected virtual async Task<T> Enqueue(T item)
        {
            LoadItem(item);
            if (InternalBuffer.Count >= BufferSize) _semaphore.WaitOne();
            InternalBuffer.TryAdd(item);
            return item;
        }

        public void ConfigureThrottling(int bufferSize, float fetchThreasholdRatio)
        {
            if (bufferSize < 1)
                throw new DataflowException("bufferSize must be a positive number");

            IdealBufferSize = bufferSize;
            BufferSize = bufferSize * 2;
            FetchThreasholdRatio = fetchThreasholdRatio;
        }

        private void EnsureFetchTaskIsRunning()
        {
            if (_fetchTask != null)
                return;

            _fetchTask = Task.Run(async () =>
            {
                try
                {
                    await FetchLoop();
                }
                catch (Exception ex)
                {
                    ErrorHandler.HandleException(ex);
                }
            });
        }

        private IQueryable<T> GetData(int numberOfRecordsToLoad)
        {
            _dataLoadSemaphore.WaitOne();
            return _getDataFunc(numberOfRecordsToLoad);
        }

        private async Task FetchLoop()
        {
            while (true)
            {
                _fetchEvent.WaitOne();

                if (_disposing)
                    break;

                var maxElementsToAdd = Math.Min(BufferSize - InternalBuffer.Count, IdealBufferSize);
                var loadedItems = _getDataFunc == null ? new List<T>().AsQueryable() : GetData(maxElementsToAdd);

                if (!loadedItems.Any()) continue;

                _dataLoadSemaphore.Reset();
                //var availableItems = await ThrottleLoadedItemsAsync(loadedItems);
                foreach (var item in loadedItems)
                    //semaphore.Wait();
                    await Enqueue(item);
                _dataLoadSemaphore.Set();
                if (DataFound != null)
                    await DataFound.Invoke(this, new DataSourceEventArgs<object> { Items = loadedItems.ToArray() });

                _signal.Set();
            }
        }

        private static IEnumerable<bool> IterateUntilTrue(Func<bool> condition)
        {
            while (!condition()) yield return true;
        }

        public async Task<T[]> ThrottleLoadedItemsAsync(IEnumerable<T> loadedItems, int maxConcurrentTasks = 5,
            int maxDegreeOfParallelism = 2)
        {
            var blockingQueue = new BlockingCollection<T>(new ConcurrentBag<T>());
            var semaphore = new SemaphoreSlim(maxConcurrentTasks);

            var t = Task.Run(() =>
            {
                foreach (var item in loadedItems)
                {
                    semaphore.Wait();
                    blockingQueue.Add(item);
                }

                blockingQueue.CompleteAdding();
            });

            var taskList = new List<Task<T>>();

            Parallel.ForEach(IterateUntilTrue(() => blockingQueue.IsCompleted),
                new ParallelOptions { MaxDegreeOfParallelism = maxDegreeOfParallelism },
                async _ =>
                {
                    if (blockingQueue.TryTake(out var item, 100))
                        taskList.Add(
                            await Enqueue(item)
                                .ContinueWith(tsk =>
                                    {
                                        semaphore.Release();
                                        return tsk;
                                    }
                                )
                        );
                });

            return await Task.WhenAll(taskList);
        }

        protected virtual void LoadItem(T item)
        {
        }

        protected virtual IProducerConsumerCollection<T> CreateBuffer()
        {
            return new ConcurrentQueue<T>();
        }

        private TransformBlock<T, T> CreatePropagationBlock()
        {
            var delay = TimeSpan.FromMilliseconds(1000);
            var lastItem = DateTime.MinValue;
            return new TransformBlock<T, T>(
                async x =>
                {
                    _propagatorIsBusy = true;
                    var waitTime = lastItem + delay - DateTime.UtcNow;
                    if (waitTime > TimeSpan.Zero)
                        await Task.Delay(waitTime);

                    lastItem = DateTime.UtcNow;
                    _propagatorIsBusy = false;
                    return x;
                },
                new ExecutionDataflowBlockOptions { MaxDegreeOfParallelism = 1 });
        }
    }
}