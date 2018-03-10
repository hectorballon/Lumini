using System.Collections.Generic;
using System.Threading;
using Lumini.Concurrent.Helpers;
using Lumini.Concurrent.Tasks;

namespace Lumini.Concurrent.Collections
{
    public class PrioritizedQueue<T> : WaitHandle
        where T : class
    {
        private readonly Queue<T> _queue = new Queue<T>();
        private readonly TaskEventWaitHandle _itemWasReceivedSignal;

        internal PrioritizedQueue(string resetEventName, BackgroundServiceTask<T> serviceTask, ushort priority)
        {
            _itemWasReceivedSignal = new TaskEventWaitHandle(false, resetEventName, priority);
            SafeWaitHandle = _itemWasReceivedSignal.SafeWaitHandle;
            serviceTask.AddResetEvent(_itemWasReceivedSignal);
            _itemWasReceivedSignal.Queue = this;
        }

        public void Enqueue(T item)
        {
            lock (_queue)
            {
                _queue.Enqueue(item);
                _itemWasReceivedSignal.Set();
            }
        }

        public T Dequeue()
        {
            lock (_queue)
            {
                var item = _queue.Dequeue();
                if (_queue.Count == 0)
                    _itemWasReceivedSignal.Reset();
                return item;
            }
        }
    }
}