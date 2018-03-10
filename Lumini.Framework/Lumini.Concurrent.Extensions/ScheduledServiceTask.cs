using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Lumini.Concurrent.Tasks;
using Lumini.Scheduled;
using Lumini.Scheduled.Extensions;
using Microsoft.Extensions.Logging;
using SystemTasks = System.Threading.Tasks;

namespace Lumini.Concurrent.Extensions
{
    public sealed class ScheduledServiceTask : BaseServiceTask
    {
        public delegate IEnumerable<IMergeableJob> GetJobsAndSchedulesDelegate();

        private readonly JobScheduler _jobScheduler;
        private readonly IList<TaskTrigger> _triggers;

        public GetJobsAndSchedulesDelegate GetJobsAndSchedules;

        public ScheduledServiceTask(IServiceConfiguration configuration, ILogger logger) : base(configuration)
        {
            _jobScheduler = new JobScheduler(logger);
            _triggers = new List<TaskTrigger>();
        }

        protected override async SystemTasks.Task StartTask(CancellationToken token)
        {
            if (GetJobsAndSchedules == null)
                throw new NullReferenceException(nameof(GetJobsAndSchedules));
            await base.StartTask(token);
        }

        private async void LoadJobsAndSchedules()
        {
            var jobs = GetJobsAndSchedules();
            foreach (var job in jobs)
                await LoadJob(job);
        }

        private async SystemTasks.Task LoadJob(IMergeableJob job)
        {
            var loadJob = new Func<IMergeableJob>(() =>
            {
                return !_jobScheduler.Exists(j => j.Name.Equals(job.Name)) ? AddJob(job) : ReplaceJob(job);
            });

            if (_jobScheduler.Exists(j => j.Name.Equals(job.Name))) return;
            var loadedJob = loadJob();
            await AddTrigger(loadedJob);
        }

        private async SystemTasks.Task AddTrigger(IMergeableJob job)
        {
            await AddTrigger(job.GetNextOccurrence());
        }

        private async SystemTasks.Task AddTrigger(Occurrence occurrence)
        {
            var newTrigger = CreateTaskTrigger(occurrence);
            _triggers.Add(newTrigger);
            await newTrigger.RunAync(CancellationToken.None)
                .ContinueWith(async _ =>
                {
                    await AddTrigger(occurrence.GetNextOccurrence());
                    RemoveTrigger(newTrigger);
                });
        }

        private static TaskTrigger CreateTaskTrigger(Occurrence occurrence)
        {
            return new TaskTrigger(new JobExecutionContext(occurrence));
        }

        private IMergeableJob AddJob(IMergeableJob job)
        {
            _jobScheduler.Add(job);
            return job;
        }

        private IMergeableJob ReplaceJob(IJob job)
        {
            var existingJob = _jobScheduler.Find(j => j == job);
            var newJob = (IMergeableJob)existingJob.MergeWith(job);
            RemoveRelatedTrigger(job);
            if (_jobScheduler.Remove(existingJob))
                _jobScheduler.Add(newJob);
            return newJob;
        }

        private void RemoveRelatedTrigger(IJob job)
        {
            var trigger = _triggers.First(t => t.Context.JobName.Equals(job.Name));
            RemoveTrigger(trigger);
        }

        private void RemoveTrigger(TaskTrigger trigger)
        {
            _triggers.Remove(trigger);
        }

        protected override async SystemTasks.Task DoWork(CancellationToken token)
        {
            await PeriodicTask.Start(LoadJobsAndSchedules,
                intervalInMilliseconds: TimeSpan.FromMinutes(5).TotalMilliseconds,
                cancelToken: token);
        }
    }
}