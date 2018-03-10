using System;
using System.Collections.Generic;

namespace Lumini.Scheduled
{
    public sealed class Occurrence : IEquatable<Occurrence>
    {
        internal Occurrence(IJob job, IEnumerable<ISchedule> schedules, DateTime startTime)
        {
            Job = job;
            Schedules = schedules;
            StartTime = startTime;
        }

        internal Occurrence(IJob job, ISchedule schedule, DateTime startTime)
            : this(job, new List<ISchedule> {schedule}, startTime)
        {
        }

        internal Occurrence(IJob job, DateTime startTime)
        {
            Job = job;
            StartTime = startTime;
        }

        public IJob Job { get; }

        public IEnumerable<ISchedule> Schedules { get; }

        public DateTime StartTime { get; }

        public bool Equals(Occurrence other)
        {
            return StartTime.Equals(other.StartTime) && Job.Task.Equals(other.Job.Task);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            return obj.GetType() == GetType() && Equals((Occurrence) obj);
        }

        public override int GetHashCode()
        {
            return StartTime.GetHashCode();
        }
    }
}