using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Lumini.Common;
using Lumini.Concurrent.LoadBalancing;
using Lumini.Concurrent.Models;

namespace Lumini.Concurrent
{

    public class LoadBalancingBroker<TLoadBalancer, TEntity> : List<Worker<TEntity>>
        where TLoadBalancer : class, ILoadBalancer
        where TEntity : class
    {
        private static ILogger _logger;

        public LoadBalancingBroker(ILogger logger, string name, DoWorkDelegate doWork)
            : this(logger, name, doWork, new LoadBalancingSettings())
        {
        }

        public LoadBalancingBroker(ILogger logger, string name, DoWorkDelegate doWork, LoadBalancingSettings settings)
        {
            _logger = logger;
            Name = name;
            Settings = settings;
            InitializeWorkerList(doWork);
            Settings.LoadBalancer = InstantiateLoadBalancer();
            ((AbstractLoadBalancer)Settings.LoadBalancer).SetWorkers(new LinkedList<IWorker>(this));
        }

        public string Name { get; }

        public int NumberOfWorkers => Settings.NumberOfWorkers;

        public int BoundedCapacityByWorker => Settings.BoundedCapacityByWorker;

        public ILoadBalancer LoadBalancer => Settings.LoadBalancer;

        private LoadBalancingSettings Settings { get; }

        private TLoadBalancer InstantiateLoadBalancer()
        {
            try
            {
                return (TLoadBalancer)Activator.CreateInstance(typeof(TLoadBalancer), _logger);
            }
            catch (Exception e1)
            {
                try
                {
                    return (TLoadBalancer)typeof(TLoadBalancer)
                        .GetConstructor(
                            BindingFlags.NonPublic | BindingFlags.CreateInstance | BindingFlags.Instance,
                            null,
                            new[] { typeof(ILogger) },
                            null
                        )
                        .Invoke(new object[] { _logger });
                }
                catch (Exception e)
                {
                    _logger?.Log(e);
                    throw;
                }
            }
        }

        private void InitializeWorkerList(DoWorkDelegate doWork)
        {
            AddRange(
                Enumerable.Range(0, Settings.NumberOfWorkers).Select(index =>
                    new Worker<TEntity>(index, Settings.BoundedCapacityByWorker, Settings.CancellationToken)
                    {
                        DoWork = doWork
                    }).ToList());
        }

        public void WaitForCompletion()
        {
            foreach (var worker in this)
                worker.WaitForCompletion();
        }

        public async Task<int> TrySendItem(object item)
        {
            return await LoadBalancer.SendItemAsync(item);
        }
    }
}