using Lumini.Common;
using Lumini.Concurrent.LoadBalancing;
using Lumini.Concurrent.Models;

namespace Lumini.Concurrent.Tasks
{
    public class RoundRobinServiceTask : LoadBalancedServiceTask<RoundRobin, object>
    {
        public RoundRobinServiceTask(IServiceConfiguration configuration, ILogger logger)
            : base(configuration, logger)
        {
        }

        protected override LoadBalancingBroker<RoundRobin, object> CreateLoadBalancingBroker()
        {
            return new LoadBalancingBroker<RoundRobin, object>(Logger,
                $"{nameof(RoundRobinServiceTask)}=>{Name}", ProcessItem,
                new LoadBalancingSettings(Settings.NumberOfWorkers, Settings.WorkerBatchSize));
        }
    }
}