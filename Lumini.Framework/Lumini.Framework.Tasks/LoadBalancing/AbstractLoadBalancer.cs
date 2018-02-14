using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Lumini.Framework.Common;

namespace Lumini.Framework.Tasks.LoadBalancing
{
    public abstract class AbstractLoadBalancer : ILoadBalancer
    {
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
                ErrorHandler.HandleException(e);
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