using System;
using System.Threading;
using SystemTasks = System.Threading.Tasks;

namespace Lumini.Scheduled
{
    public sealed class TaskTrigger : ITaskTrigger
    {
        public TaskTrigger(Occurrence occurence)
            : this(new JobExecutionContext(occurence))
        {
        }

        public TaskTrigger(JobExecutionContext context)
        {
            Context = context;
        }

        public JobExecutionContext Context { get; }

        public event TaskTriggerEventHandler BeforeExecution;
        public event TaskTriggerEventHandler ExecutionCompleted;

        public void Run(CancellationToken token)
        {
            if (!CanBeExecuted()) return;
            Context.Result = this.Execute(RunTask, CalculateElapsedTimeSpan().TotalMilliseconds, token);
        }

        public async SystemTasks.Task RunAync(CancellationToken token)
        {
            if (!CanBeExecuted()) return;
            Context.Result = await this.ExecuteAsync(RunTask, CalculateElapsedTimeSpan().TotalMilliseconds, token);
        }

        private TimeSpan CalculateElapsedTimeSpan()
        {
            var elapsedTimeSpan = TimeSpan.Zero;
            if (Context.ExecutionScheduledFor > DateTime.UtcNow)
                elapsedTimeSpan =
                    Context.ExecutionScheduledFor - DateTime.UtcNow;
            return elapsedTimeSpan;
        }

        private bool CanBeExecuted()
        {
            var canBeExecuted = Context.Task.CanBeExecuted();
            if (!canBeExecuted) Context.Result = TaskResult.ValidationFailed;
            return canBeExecuted;
        }

        private TaskResult RunTask()
        {
            try
            {
                BeforeExecution?.Invoke(this, new JobEventArgs { Context = Context });
                Context.ExecutionStartedOn = DateTime.UtcNow;
                var result = Context.Task.Run(Context.JobName, Context.ScheduleName, Context.Parameters);
                Context.ExecutionFinishedOn = DateTime.UtcNow;
                ExecutionCompleted?.Invoke(this, new JobEventArgs { Context = Context });
                return result;
            }
            catch (Exception e)
            {
                JobScheduler.HandleException(e);
                return TaskResult.Aborted;
            }
        }
    }
}