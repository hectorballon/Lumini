namespace Lumini.Framework.Dataflow
{
    public struct DataSourceEventData
    {
        public DataSourceEventData(string name, IPrioritizedSource dataSource)
        {
            EventHandlerName = name;
            DataSource = dataSource;
        }

        public string EventHandlerName { get; }
        public ushort Priority => DataSource.Priority;
        public IPrioritizedSource DataSource { get; }
    }
}