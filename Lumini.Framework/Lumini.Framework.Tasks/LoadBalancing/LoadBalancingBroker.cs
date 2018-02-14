using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using Lumini.Framework.Common;

namespace Lumini.Framework.Tasks.LoadBalancing
{
    public sealed class LoadBalancingBroker<TLoadBalancer, TEntity> : List<Worker<TEntity>>
        where TLoadBalancer : class, ILoadBalancer
        where TEntity : class
    {
        public LoadBalancingBroker(DoWorkDelegate doWork)
            : this(new LoadBalancingSettings(), doWork)
        {
        }

        public LoadBalancingBroker(LoadBalancingSettings settings, DoWorkDelegate doWork)
        {
            LoadBalancingSettings = settings;
            InitializeWorkerList(doWork);
            LoadBalancingSettings.LoadBalancer = InstantiateLoadBalancer();
            ((AbstractLoadBalancer) LoadBalancingSettings.LoadBalancer).SetWorkers(new LinkedList<IWorker>(this));
        }

        public int NumberOfWorkers => LoadBalancingSettings.NumberOfWorkers;

        public int BoundedCapacityByWorker => LoadBalancingSettings.BoundedCapacityByWorker;

        public ILoadBalancer LoadBalancer => LoadBalancingSettings.LoadBalancer;

        private LoadBalancingSettings LoadBalancingSettings { get; }

        private static TLoadBalancer InstantiateLoadBalancer()
        {
            try
            {
                return (TLoadBalancer) Activator.CreateInstance(typeof(TLoadBalancer));
            }
            catch (Exception)
            {
                try
                {
                    return (TLoadBalancer) typeof(TLoadBalancer)
                        .GetConstructor(
                            BindingFlags.NonPublic | BindingFlags.CreateInstance | BindingFlags.Instance,
                            null,
                            null,
                            null
                        )
                        .Invoke(new object[] { });
                }
                catch (Exception e)
                {
                    ErrorHandler.HandleException(e);
                    throw;
                }
            }
        }

        private void InitializeWorkerList(DoWorkDelegate doWork)
        {
            for (var index = 0; index < LoadBalancingSettings.NumberOfWorkers; index++)
            {
                var worker = new Worker<TEntity>(index, 100, LoadBalancingSettings.CancellationToken) {DoWork = doWork};
                Add(worker);
            }
        }

        public void WaitForCompletion()
        {
            foreach (var worker in this)
                worker.WaitForCompletion();
        }

        public async Task<bool> TrySendItem(object item)
        {
            return await LoadBalancer.SendItemAsync(item);
        }
    }
}