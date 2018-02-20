using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Lumini.Concurrent.Helpers
{
    public class ServiceTaskThrottler
    {
        private readonly ILogger _logger;

        public ServiceTaskThrottler(ILogger logger)
        {
            _logger = logger;
        }

        private IEnumerable<bool> IterateUntilTrue(Func<bool> condition)
        {
            while (!condition()) yield return true;
        }

        public async Task ThrottleLoadedItemsAsync<T>(ConcurrentQueue<T> queue, CancellationToken token,
            Func<T, Task<bool>> taskToRun,
            Func<T, bool, Task> taskToContinueWith, int maxConcurrentTasks = 10, int maxDegreeOfParallelism = 2)
        {
            var blockingQueue = new BlockingCollection<T>(new ConcurrentBag<T>());
            var semaphore = new SemaphoreSlim(maxConcurrentTasks);

            var t = Task.Run(() =>
            {
                try
                {
                    while (true)
                    {
                        if (token.IsCancellationRequested) break;
                        semaphore.Wait(token);
                        if (queue.TryDequeue(out var item))
                            blockingQueue.Add(item, token);
                    }
                }
                catch (Exception e)
                {
                    _logger.LogError(e, e.ToString());
                }

                blockingQueue.CompleteAdding();
            }, token);

            var runningTasks = new List<Task>();

            Parallel.ForEach(IterateUntilTrue(() => blockingQueue.IsCompleted),
                new ParallelOptions { MaxDegreeOfParallelism = maxDegreeOfParallelism },
                async _ =>
                {
                    if (blockingQueue.TryTake(out var item, 100))
                        runningTasks.Add(
                            await taskToRun(item)
                                .ContinueWith(tsk =>
                                {
                                    semaphore.Release();
                                    return taskToContinueWith(item, tsk.Result);
                                }, token)
                        );
                });

            while (!token.IsCancellationRequested)
                while (runningTasks.Any(task => !task.IsCompleted))
                {
                    await Task.WhenAny(runningTasks);
                    runningTasks.RemoveAll(task => task.IsCompleted);
                }
        }
    }
}