using System.Threading;

namespace Lumini.Concurrent.Helpers
{
    public class TaskEventWaitHandle : EventWaitHandle
    {
        private TaskEventData _eventData;

        public TaskEventWaitHandle(bool initialState, string name)
            : base(initialState, EventResetMode.ManualReset, name)
        {
            _eventData = new TaskEventData(name, ushort.MaxValue);
        }

        public TaskEventWaitHandle(bool initialState, string name, ushort priority)
            : base(initialState, EventResetMode.ManualReset, name)
        {
            _eventData = new TaskEventData(name, priority);
        }

        public TaskEventWaitHandle(bool initialState, string name, ushort priority, out bool createdNew)
            : base(initialState, EventResetMode.ManualReset, name, out createdNew)
        {
            _eventData = new TaskEventData(name, priority);
        }

        public string Name => _eventData.EventHandlerName;

        public ushort Priority => _eventData.Priority;

        public WaitHandle Queue { get; internal set; }

        public TaskEventData? WaitForData()
        {
            if (WaitOne())
                return _eventData;
            return null;
        }
    }
}