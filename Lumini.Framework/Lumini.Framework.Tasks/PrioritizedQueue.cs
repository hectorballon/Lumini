using System.Collections.Generic;
using System.Threading;
using Lumini.Framework.Common;

namespace Lumini.Framework.Tasks
{
    public class PrioritizedQueue<T> : WaitHandle
        where T : IThreadable
    {
        private readonly Queue<T> _queue = new Queue<T>();
        private readonly TaskEventWaitHandle _signal;

        internal PrioritizedQueue(string resetEventName, PrioritizedQueueServiceTask<T> serviceTask, ushort priority)
        {
            _signal = new TaskEventWaitHandle(false, resetEventName, priority);
            SafeWaitHandle = _signal.SafeWaitHandle;
            serviceTask.AddResetEvent(_signal);
            _signal.Queue = this;
        }

        public void Enqueue(T item)
        {
            lock (_queue)
            {
                _queue.Enqueue(item);
                _signal.Set();
            }
        }

        public T Dequeue()
        {
            lock (_queue)
            {
                var item = _queue.Dequeue();
                if (_queue.Count == 0)
                    _signal.Reset();
                return item;
            }
        }
    }
}