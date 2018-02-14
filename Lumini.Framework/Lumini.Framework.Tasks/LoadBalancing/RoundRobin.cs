using System.Collections.Generic;

namespace Lumini.Framework.Tasks.LoadBalancing
{
    public class RoundRobin : AbstractLoadBalancer
    {
        private LinkedListNode<IWorker> _currentWorker;

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