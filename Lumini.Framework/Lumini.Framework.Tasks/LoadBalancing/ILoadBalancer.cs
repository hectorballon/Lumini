using System.Collections.Generic;
using System.Threading.Tasks;

namespace Lumini.Framework.Tasks.LoadBalancing
{
    public interface ILoadBalancer
    {
        LinkedList<IWorker> Workers { get; }
        Task<bool> SendItemAsync(object itemToProcess);
    }
}