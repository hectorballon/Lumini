using Lumini.Common;
using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;

namespace Lumini.Concurrent.Tasks
{
    public abstract class LoadBalancedServiceTask<TLoadBalancer, TEntity> : BaseServiceTask
        where TLoadBalancer : class, ILoadBalancer
        where TEntity : class
    {
        private readonly ManualResetEvent _itemAvailableEvent;

        protected LoadBalancedServiceTask(IServiceConfiguration configuration, ILogger logger)
            : base(configuration)
        {
            _itemAvailableEvent = new ManualResetEvent(false);
            Logger = logger;
        }

        public DoWorkDelegate ProcessItem { get; set; }

        protected LoadBalancingBroker<TLoadBalancer, TEntity> Broker { get; set; }
        protected ILoadBalancer LoadBalancer => Broker?.LoadBalancer;
        protected virtual IProducerConsumerCollection<TEntity> InternalBuffer { get; private set; }
        protected virtual IPropagatorBlock<TEntity, TEntity> Propagator { get; private set; }

        protected ILogger Logger { get; }

        public virtual void Enqueue(TEntity item)
        {
            if (InternalBuffer == null || InternalBuffer.Count >= Settings.WorkerBatchSize)
            {
                _itemAvailableEvent.Reset();
                _itemAvailableEvent.WaitOne();
            }
            InternalBuffer.TryAdd(item);
        }

        protected abstract LoadBalancingBroker<TLoadBalancer, TEntity> CreateLoadBalancingBroker();

        public override async Task Start(CancellationToken token)
        {
            if (Propagator != null && !Propagator.Completion.IsCompleted)
                throw new Exception("You must call Stop method first, as the previous run is still pending");

            StartBuffering();
            var feederTask = Task.Run(async () =>
            {
                while (true)
                {
                    if (token.IsCancellationRequested) break;
                    if (!InternalBuffer.TryTake(out var item))
                    {
                        Thread.Sleep(2000);
                        continue;
                    }
                    _itemAvailableEvent.Set();
                    await Propagator.SendAsync(item, token);
                }
            }, token);
            await base.Start(token);
        }

        private void StartBuffering()
        {
            InternalBuffer = CreateBuffer();
            Propagator = CreatePropagationBlock();
            _itemAvailableEvent.Set();
        }

        protected virtual IProducerConsumerCollection<TEntity> CreateBuffer()
        {
            return new ConcurrentQueue<TEntity>();
        }

        protected virtual IPropagatorBlock<TEntity, TEntity> CreatePropagationBlock()
        {
            var delay = TimeSpan.FromMilliseconds(Settings.PropagationDelayInMilliseconds);
            var lastItem = DateTime.MinValue;
            return new TransformBlock<TEntity, TEntity>(
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

        public virtual async Task<TEntity> GetNextItemAsync(CancellationToken token)
        {
            return await Propagator.ReceiveAsync(token);
        }

        protected override async Task DoWork(CancellationToken token)
        {
            if (ProcessItem == null) throw new ArgumentNullException($"Method {nameof(BasicServiceTask)}->{nameof(ProcessItem)} was not defined");
            if (Broker == null) Broker = CreateLoadBalancingBroker();
            token.Register(async () => await Stop());
            while (!token.IsCancellationRequested)
            {
                if (!Settings.Enabled)
                {
                    Status = Enums.TaskStatus.Idle;
                    await Task.Delay(TimeSpan.FromDays(1).Milliseconds, token);
                    continue;
                }
                Status = Enums.TaskStatus.Running;
                var item = await GetNextItemAsync(token);
                if (item == null)
                {
                    Status = Enums.TaskStatus.Idle;
                    await Task.Delay(TimeSpan.FromSeconds(Settings.IdleTimeInMilliseconds), token);
                    continue;
                }
                await LoadBalancer.SendItemAsync(item);
                await Task.Delay(Settings.IdleTimeInMilliseconds, token);
            }
        }
    }
}