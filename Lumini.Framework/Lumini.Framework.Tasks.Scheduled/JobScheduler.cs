using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Lumini.Framework.Tasks.Scheduled.Extensions;

namespace Lumini.Framework.Tasks.Scheduled
{
    public sealed class JobScheduler : List<IMergeableJob>
    {
        private readonly List<System.Threading.Tasks.Task> _runningTasks;
        private bool _started;

        public JobScheduler()
        {
            _runningTasks = new List<System.Threading.Tasks.Task>();
            _started = false;
        }

        public HashSet<Occurrence> GetNextOccurrences()
        {
            var occurrences = new HashSet<Occurrence>();
            foreach (var job in this)
                occurrences.Add(job.GetNextOccurrence());
            return occurrences;
        }

        public HashSet<Occurrence> GetNextOccurrences(DateTime startDate, DateTime endDate)
        {
            var occurrences = new HashSet<Occurrence>();
            foreach (var job in this)
                occurrences.UnionWith(job.GetNextOccurrences(endDate, startDate));
            return occurrences;
        }

        public HashSet<Occurrence> GetNextNOccurrences(int numberOfOccurrences)
        {
            var occurrences = new HashSet<Occurrence>();
            foreach (var job in this)
                occurrences.UnionWith(job.GetNextNOccurrences(numberOfOccurrences));
            return occurrences;
        }

        public async System.Threading.Tasks.Task Start(CancellationToken token)
        {
            if (!_started)
                while (!token.IsCancellationRequested)
                {
                    StartJobs();
                    await RunAsyncUntilIdle();
                    await System.Threading.Tasks.Task.Delay(TimeSpan.FromSeconds(1));
                }
        }

        private async System.Threading.Tasks.Task RunAsyncUntilIdle()
        {
            while (_runningTasks.Any(t => !t.IsCompleted))
            {
                await System.Threading.Tasks.Task.WhenAll(_runningTasks);
                _runningTasks.RemoveAll(t => t.IsCompleted);
                foreach (var job in this)
                    if (job.CurrentTask != null && !_runningTasks.Contains(job.CurrentTask))
                        _runningTasks.Add(job.CurrentTask);
            }
        }

        public void LoadJobs(IEnumerable<IMergeableJob> jobs)
        {
            foreach (var job in jobs)
                LoadJob(job);
        }

        public T LoadJob<T>(T job)
            where T : class, IMergeableJob
        {
            var loadJob = new Func<IMergeableJob>(() =>
            {
                return !Exists(j => j.Name.Equals(job.Name)) ? AddJob(job) : ReplaceJob(job);
            });

            var loadedJob = (T) loadJob();
            return loadedJob;
        }

        private IMergeableJob AddJob(IMergeableJob job)
        {
            Add(job);
            StartJob(job);
            return job;
        }

        private IMergeableJob ReplaceJob(IMergeableJob job)
        {
            var existingJob = Find(j => j.Equals(job));
            var newJob = (IMergeableJob) existingJob.MergeWith(job);
            AddJob(newJob);
            RemoveJob(existingJob);
            return newJob;
        }

        private void RemoveJob(IMergeableJob job)
        {
            if (_started)
            {
                RemoveRelatedTrigger(job);
                job.Stop();
            }
            Remove(job);
        }

        private void RemoveRelatedTrigger(IMergeableJob job)
        {
            _runningTasks.Remove(job.CurrentTask);
        }

        public void StartJobs()
        {
            Stop();
            _started = true;
            foreach (var job in this)
                StartJob(job);
        }

        private void StartJob(IMergeableJob job)
        {
            if (!_started) return;
            var task = job.Start();
            if (task != null)
                _runningTasks.Add(task);
        }


        public void Stop()
        {
            if (!_started) return;
            if (_runningTasks.Count > 0)
                _runningTasks.Clear();
            _started = false;
        }
    }
}