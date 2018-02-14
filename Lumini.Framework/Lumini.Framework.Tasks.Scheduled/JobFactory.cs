using System;
using System.Collections.Generic;
using System.Reflection;
using Lumini.Framework.Common;

namespace Lumini.Framework.Tasks.Scheduled
{
    public static class JobFactory
    {
        public static JobBuilder<T> Create<T>(string jobName)
            where T : class, IJob, IMergeable
        {
            return new JobBuilder<T>().CreateJob(jobName);
        }

        public class JobBuilder<TJob>
            where TJob : class, IJob, IMergeable
        {
            private readonly List<ISchedule> _schedules;
            private string _jobName;
            private Task _task;

            internal JobBuilder()
            {
                _schedules = new List<ISchedule>();
            }

            internal JobBuilder<TJob> CreateJob(string jobName)
            {
                _jobName = jobName;
                return this;
            }

            public JobBuilder<TJob> ForTask(Task task)
            {
                _task = task;
                return this;
            }

            public JobBuilder<TJob> UsingSchedule(string name, string cronExpression)
            {
                _schedules.Add(new Schedule(name, cronExpression));
                return this;
            }

            public JobBuilder<TJob> UsingSchedules(params ISchedule[] schedules)
            {
                foreach (var schedule in schedules)
                    _schedules.Add(schedule);
                return this;
            }

            public TJob Build()
            {
                var newJob = CreateInstance();
                newJob.Task = _task;
                foreach (var schedule in _schedules)
                    newJob.Schedules.Add(schedule);

                return newJob;
            }

            public TJob BuildFrom(IJob job)
            {
                var newJob = CreateInstance();
                return (TJob) newJob.MergeWith(job);
            }

            private TJob CreateInstance()
            {
                try
                {
                    return (TJob) Activator.CreateInstance(typeof(TJob), _jobName);
                }
                catch (Exception e)
                {
                    return (TJob) typeof(TJob)
                        .GetConstructor(
                            BindingFlags.NonPublic | BindingFlags.CreateInstance | BindingFlags.Instance,
                            null,
                            new[] {typeof(string)},
                            null
                        )
                        .Invoke(new object[] {_jobName});
                }
            }
        }
    }
}