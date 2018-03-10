using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Lumini.Common;

namespace Lumini.Concurrent.LoadBalancing
{
    public abstract class AbstractLoadBalancer : ILoadBalancer
    {
        private readonly ILogger _logger;

        protected AbstractLoadBalancer(ILogger logger)
        {
            _logger = logger;
        }

        public virtual async Task<int> SendItemAsync(object itemToProcess)
        {
            try
            {
                if (!Workers.Any()) throw new Exception("No workers available!");
                if (itemToProcess == null) throw new ArgumentNullException(nameof(itemToProcess));
                var worker = SelectWorker();
                while (!worker.CanReceiveItems()) Thread.Sleep(2000);
                await worker.ReceiveAsync(itemToProcess);
                return worker.WorkerId;
            }
            catch (Exception e)
            {
                _logger?.Log(e);
                return -1;
            }
        }

        public LinkedList<IWorker> Workers { get; private set; }

        protected abstract IWorker SelectWorker();

        internal void SetWorkers(LinkedList<IWorker> workers)
        {
            Workers = workers;
        }
    }
}