using System;
using System.Threading;

namespace Lumini.Concurrent.Models
{
    public class WorkerStats
    {
        private int _itemsInQueue;
        private int _itemsSucceeded;
        private int _itemsWithError;

        public WorkerStats()
        {
            ActivityDurations = new ActivityDurations();
            StartDate = DateTime.UtcNow;
        }

        public int ItemsProcessed => _itemsSucceeded + _itemsWithError;
        public int ItemsInQueue => _itemsInQueue;
        public int ItemsSucceeded => _itemsSucceeded;
        public int ItemsWithError => _itemsWithError;
        public DateTime StartDate { get; }
        public DateTime EndDate { get; internal set; }
        public ActivityDurations ActivityDurations { get; internal set; }

        public void IncrementItemsInQueue()
        {
            Interlocked.Increment(ref _itemsInQueue);
        }

        public void IncrementItemsSucceeded()
        {
            Interlocked.Increment(ref _itemsSucceeded);
        }

        public void IncrementItemsWithError()
        {
            Interlocked.Increment(ref _itemsWithError);
        }

        public void DecrementItemsInQueue()
        {
            Interlocked.Decrement(ref _itemsInQueue);
        }
    }
}