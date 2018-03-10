using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Lumini.Scheduled.Extensions;

namespace Lumini.Scheduled
{
    public class Job : IMergeableJob, ICloneable
    {
        private CancellationTokenSource _cts;

        private Job() : this(string.Empty)
        {
        }

        protected Job(string name)
        {
            Id = Guid.NewGuid();
            Name = name;
            Schedules = new List<ISchedule>();
            Enabled = true;
        }

        private TaskTrigger Trigger { get; set; }

        public object Clone()
        {
            return _cts.IsCancellationRequested ? null : this.Copy();
        }

        public Guid Id { get; private set; }
        public DateTime? LastExecution { get; set; }
        public DateTime? NextExecution { get; set; }
        public bool IsRunning { get; set; }
        public bool Enabled { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public TaskResult LastResult { get; set; }
        public bool ForceExecution { get; set; }
        public JobPriority Priority { get; set; }
        public JobType Type => Schedules.Count > 0 ? JobType.Scheduled : JobType.OnDemand;
        public Task Task { get; set; }
        public IList<ISchedule> Schedules { get; set; }

        public bool Equals(IJob other)
        {
            if (_cts.IsCancellationRequested) return false;
            if (other == null || GetType() != other.GetType())
                return false;
            return other.Name.Equals(Name) && other.Type.Equals(Type) &&
                   other.Task.GetType() == Task.GetType();
        }

        public bool CanMergeWith(object obj)
        {
            if (!(obj is IJob job))
                return false;

            return job.Task.GetType() == Task.GetType();
        }

        public IMergeable MergeWith(object obj)
        {
            if (_cts.IsCancellationRequested) return null;

            if (!CanMergeWith(obj)) return null;

            if (!(obj is IJob job)) return null;

            var newJob = new Job
            {
                Id = Id,
                Name = Name,
                Task = job.Task,
                Priority = job.Priority,
                LastExecution =
                    new List<DateTime> { LastExecution ?? DateTime.MinValue, job.LastExecution ?? DateTime.MinValue }
                        .Max(),
                NextExecution = job.NextExecution ?? NextExecution,
                LastResult = job.LastResult,
                Enabled = job.Enabled
            };
            if (newJob.LastExecution == DateTime.MinValue) newJob.LastExecution = null;

            foreach (var schedule in job.Schedules)
                newJob.Schedules.Add(new Schedule(schedule.Name, schedule.ToString()));

            return newJob;
        }

        public void Stop()
        {
            _cts?.Cancel();
            Trigger = null;
            CurrentTask = null;
        }

        public System.Threading.Tasks.Task Start()
        {
            if (CurrentTask != null) return CurrentTask;
            _cts = new CancellationTokenSource();
            StartTask(this.GetNextOccurrence());
            return CurrentTask;
        }

        public System.Threading.Tasks.Task CurrentTask { get; private set; }

        public static bool operator ==(Job job1, Job job2)
        {
            return job1 != null && job1.Equals(job2);
        }

        public static bool operator !=(Job job1, Job job2)
        {
            return !(job1 == job2);
        }

        private void StartTask(Occurrence occurrence)
        {
            try
            {
                if (_cts.IsCancellationRequested) return;
                Trigger = GetTriggerForNextOccurrence(occurrence);
                CurrentTask = GetRunningTaskForTrigger(occurrence);
            }
            catch (Exception e)
            {
                JobScheduler.HandleException(e);
            }
        }

        private TaskTrigger GetTriggerForNextOccurrence(Occurrence occurrence)
        {
            if (_cts.IsCancellationRequested || occurrence == null) return null;
            if (!Enabled || occurrence.StartTime < DateTime.UtcNow) return null;
            return new TaskTrigger(occurrence);
        }

        private System.Threading.Tasks.Task GetRunningTaskForTrigger(Occurrence occurrence)
        {
            var runningTask = Trigger?.RunAync(_cts.Token);
            return runningTask?.ContinueWith(_ =>
                StartTask(CalculateNextOccurrence(occurrence)));
        }

        private Occurrence CalculateNextOccurrence(Occurrence occurrence)
        {
            if (Type == JobType.OnDemand) return null;
            var nextOccurrence = Trigger?.Context.GetNextOccurrence();
            if (nextOccurrence == null || nextOccurrence.StartTime.Equals(occurrence.StartTime))
                nextOccurrence = null;
            return nextOccurrence;
        }
    }
}