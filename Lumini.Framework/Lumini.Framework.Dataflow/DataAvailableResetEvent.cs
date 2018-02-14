using System.Threading;

namespace Lumini.Framework.Dataflow
{
    public class DataAvailableResetEvent : EventWaitHandle
    {
        private DataSourceEventData _eventData;

        internal DataAvailableResetEvent(bool initialState, string name, IPrioritizedSource dataSource)
            : base(initialState, EventResetMode.ManualReset, name)
        {
            _eventData = new DataSourceEventData(name, dataSource);
        }

        public string Name => _eventData.EventHandlerName;

        public ushort Priority => _eventData.Priority;

        public IPrioritizedSource DataSource => _eventData.DataSource;

        public DataSourceEventData? WaitForData()
        {
            if (WaitOne())
                return _eventData;
            return null;
        }
    }
}