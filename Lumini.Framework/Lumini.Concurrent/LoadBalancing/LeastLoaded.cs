using System;
using System.Linq;
using Microsoft.Extensions.Logging;

namespace Lumini.Concurrent.LoadBalancing
{
    public class LeastLoaded : AbstractLoadBalancer
    {
        public LeastLoaded(ILogger logger) : base(logger)
        {
        }

        protected override IWorker SelectWorker()
        {
            lock (new object())
            {
                return (from worker in Workers
                        orderby worker.GetStats().ItemsInQueue, worker.GetLastTimeAssigned() ?? DateTime.UtcNow
                        select worker).FirstOrDefault();
            }
        }
    }
}