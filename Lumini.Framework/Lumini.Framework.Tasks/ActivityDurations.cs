namespace Lumini.Framework.Tasks
{
    public class ActivityDurations
    {
        public ActivityDurations()
        {
            MinTimeInMs = double.MaxValue;
        }

        public double MaxTimeInMs { get; internal set; }
        public double MinTimeInMs { get; internal set; }
        public double AverageTimeInMs { get; internal set; }
        public double TotalTimeInMs { get; internal set; }
    }
}