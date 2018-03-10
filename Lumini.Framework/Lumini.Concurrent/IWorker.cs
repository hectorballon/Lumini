using System;
using System.Threading.Tasks;
using Lumini.Concurrent.Models;

namespace Lumini.Concurrent
{
    public delegate Task<bool> DoWorkDelegate(object item, int instanceId);

    public interface IWorker
    {
        int WorkerId { get; }
        bool CanReceiveItems();
        Task ReceiveAsync(object item);
        void WaitForCompletion();
        WorkerStats GetStats();
        DateTime? GetLastTimeAssigned();
    }
}