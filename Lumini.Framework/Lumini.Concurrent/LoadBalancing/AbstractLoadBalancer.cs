using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Lumini.Concurrent.LoadBalancing
{
    public abstract class AbstractLoadBalancer : ILoadBalancer
    {
        private readonly ILogger _logger;

        protected AbstractLoadBalancer(ILogger logger)
        {
            _logger = logger;
        }

        public virtual async Task<bool> SendItemAsync(object itemToProcess)
        {
            try
            {
                if (!Workers.Any()) throw new Exception("No workers available!");
                if (itemToProcess == null) throw new ArgumentNullException(nameof(itemToProcess));
                var worker = SelectWorker();
                while (!worker.CanReceiveItems()) Thread.Sleep(2000);
                await worker.ReceiveAsync(itemToProcess);
                return true;
            }
            catch (Exception e)
            {
                _logger.LogError(e, e.ToString());
                return false;
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