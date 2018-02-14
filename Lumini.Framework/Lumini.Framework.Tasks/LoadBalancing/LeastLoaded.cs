using System;
using System.Linq;

namespace Lumini.Framework.Tasks.LoadBalancing
{
    public class LeastLoaded : AbstractLoadBalancer
    {
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