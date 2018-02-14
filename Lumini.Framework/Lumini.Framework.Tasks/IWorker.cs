using System;
using System.Threading.Tasks;

namespace Lumini.Framework.Tasks
{
    public delegate Task<bool> DoWorkDelegate(object item);

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