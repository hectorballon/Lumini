using System;
using System.Collections.Generic;
using System.Linq;
using Lumini.Framework.Tasks.Scheduled.Extensions;

namespace Lumini.Framework.Tasks.Scheduled
{
    public sealed class JobExecutionContext
    {
        internal JobExecutionContext(Occurrence occurrence)
        {
            Occurrence = occurrence;
            Parameters = new Dictionary<string, object>();
        }

        private Occurrence Occurrence { get; }

        public string JobName => Occurrence.Job.Name;
        public string ScheduleName => Occurrence.Schedules != null ? Occurrence.Schedules.First().Name : JobName;
        public JobType JobType => Occurrence.Job.Type;
        public Task Task => Occurrence.Job.Task;
        public DateTime? LastExecutedOn => Occurrence.Job.LastExecution;
        public DateTime ExecutionStartedOn { get; internal set; }
        public DateTime ExecutionFinishedOn { get; internal set; }
        public DateTime ExecutionScheduledFor => Occurrence.StartTime;
        public bool ForcedExecution => Occurrence.Job.ForceExecution;
        public TaskResult Result { get; internal set; }
        public IDictionary<string, object> Parameters { get; }

        public Occurrence GetNextOccurrence()
        {
            return Occurrence.GetNextOccurrence();
        }

        public DateTime GetEstimatedTimeForNextExecution()
        {
            return Occurrence.GetNextOccurrence().StartTime;
        }
    }
}