using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace Lumini.Concurrent.Tasks
{
    public static class PeriodicTask
    {
        public static Task Start(Action action,
            Action<Exception> exceptionHanlder = null,
            double intervalInMilliseconds = Timeout.Infinite,
            double delayInMilliseconds = 0,
            int duration = Timeout.Infinite,
            int maxIterations = -1,
            bool synchronous = false,
            CancellationToken cancelToken = new CancellationToken(),
            TaskCreationOptions periodicTaskCreationOptions = TaskCreationOptions.None)
        {
            var stopWatch = new Stopwatch();
            var wrapperAction = new Action(() =>
            {
                CheckIfCancelled(cancelToken);
                action();
            });

            var mainAction = new Action(() =>
            {
                try
                {
                    MainPeriodicTaskAction(intervalInMilliseconds, delayInMilliseconds, duration, maxIterations,
                        cancelToken, stopWatch, synchronous, wrapperAction, periodicTaskCreationOptions);
                }
                catch (Exception e)
                {
                    exceptionHanlder?.Invoke(e);
                }
            });

            return Task.Factory.StartNew(mainAction, cancelToken, TaskCreationOptions.LongRunning,
                TaskScheduler.Current);
        }

        private static void MainPeriodicTaskAction(double intervalInMilliseconds,
            double delayInMilliseconds,
            int duration,
            int maxIterations,
            CancellationToken cancelToken,
            Stopwatch stopWatch,
            bool synchronous,
            Action wrapperAction,
            TaskCreationOptions periodicTaskCreationOptions)
        {
            var subTaskCreationOptions = TaskCreationOptions.AttachedToParent | periodicTaskCreationOptions;

            CheckIfCancelled(cancelToken);

            if (delayInMilliseconds > 0)
                Thread.Sleep(Convert.ToInt32(delayInMilliseconds));

            if (maxIterations == 0) return;

            var iteration = 0;

            using (var periodResetEvent = new ManualResetEventSlim(false))
            {
                while (true)
                {
                    CheckIfCancelled(cancelToken);

                    var subTask = Task.Factory.StartNew(wrapperAction, cancelToken, subTaskCreationOptions,
                        TaskScheduler.Current);

                    if (synchronous)
                    {
                        stopWatch.Start();
                        try
                        {
                            subTask.Wait(cancelToken);
                        }
                        catch (Exception)
                        {
                            // Ignore
                        }
                        stopWatch.Stop();
                    }

                    const double tolerance = 0;
                    if (Math.Abs(intervalInMilliseconds - Timeout.Infinite) < tolerance) break;

                    iteration++;

                    if (maxIterations > 0 && iteration >= maxIterations) break;

                    try
                    {
                        stopWatch.Start();
                        periodResetEvent.Wait(Convert.ToInt32(intervalInMilliseconds), cancelToken);
                        stopWatch.Stop();
                    }
                    catch (Exception)
                    {
                        //Ignore
                    }
                    finally
                    {
                        periodResetEvent.Reset();
                    }

                    CheckIfCancelled(cancelToken);

                    if (duration > 0 && stopWatch.ElapsedMilliseconds >= duration) break;
                }
            }
        }

        private static void CheckIfCancelled(CancellationToken cancellationToken)
        {
            if (cancellationToken == null)
                throw new ArgumentNullException(nameof(cancellationToken));

            cancellationToken.ThrowIfCancellationRequested();
        }
    }
}