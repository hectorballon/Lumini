using System.Threading;

namespace Lumini.Concurrent.Models
{
    public class LoadBalancingSettings
    {
        public LoadBalancingSettings(int numberOfWorkers = 10, int boundedCapacityByWorker = 100)
            : this(numberOfWorkers, boundedCapacityByWorker, CancellationToken.None)
        {
        }

        public LoadBalancingSettings(int numberOfWorkers, int boundedCapacityByWorker, CancellationToken token)
        {
            NumberOfWorkers = numberOfWorkers;
            BoundedCapacityByWorker = boundedCapacityByWorker;
            CancellationToken = token;
        }

        public int NumberOfWorkers { get; }
        public int BoundedCapacityByWorker { get; }
        public CancellationToken CancellationToken { get; }
        public ILoadBalancer LoadBalancer { get; internal set; }
    }
}