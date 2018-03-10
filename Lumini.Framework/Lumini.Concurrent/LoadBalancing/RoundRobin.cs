using System.Collections.Generic;
using Lumini.Common;

namespace Lumini.Concurrent.LoadBalancing
{
    public class RoundRobin : AbstractLoadBalancer
    {
        private LinkedListNode<IWorker> _currentWorker;

        public RoundRobin(ILogger logger) : base(logger)
        {
        }

        protected override IWorker SelectWorker()
        {
            lock (new object())
            {
                _currentWorker = _currentWorker?.Next ?? Workers.First;
                return _currentWorker.Value;
            }
        }
    }
}