using System.Collections.Generic;
using System.Threading.Tasks;

namespace Lumini.Concurrent
{
    public interface ILoadBalancer
    {
        LinkedList<IWorker> Workers { get; }
        Task<bool> SendItemAsync(object itemToProcess);
    }
}