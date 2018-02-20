namespace Lumini.Concurrent.Helpers
{
    public struct TaskEventData
    {
        public TaskEventData(string name, ushort priority)
        {
            EventHandlerName = name;
            Priority = priority;
        }

        public string EventHandlerName { get; }
        public ushort Priority { get; }
    }
}