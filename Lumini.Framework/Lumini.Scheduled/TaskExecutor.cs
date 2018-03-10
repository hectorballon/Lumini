using System;
using System.Threading;
using SystemTasks = System.Threading.Tasks;

namespace Lumini.Scheduled
{
    public static class TaskExecutor
    {
        public static TaskResult Execute(this ITaskTrigger job, Func<TaskResult> action,
            double timeoutInMilliseconds, CancellationToken token)
        {
            try
            {
                SystemTasks.Task.Delay(TimeSpan.FromMilliseconds(timeoutInMilliseconds), token).Wait(token);
                if (token.IsCancellationRequested) return TaskResult.Aborted;
                return action();
            }
            catch (Exception e)
            {
                return TaskResult.Aborted;
            }
        }

        public static async SystemTasks.Task<TaskResult> ExecuteAsync(this ITaskTrigger job, Func<TaskResult> action,
            double timeoutInMilliseconds, CancellationToken token)
        {
            try
            {
                await SystemTasks.Task.Delay(TimeSpan.FromMilliseconds(timeoutInMilliseconds), token);
                if (token.IsCancellationRequested) return TaskResult.Aborted;
                return await SystemTasks.Task.Run(action, token);
            }
            catch (Exception e)
            {
                return TaskResult.Aborted;
            }
        }
    }
}