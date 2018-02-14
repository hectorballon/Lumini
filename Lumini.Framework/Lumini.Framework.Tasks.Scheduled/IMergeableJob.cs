using Lumini.Framework.Common;

namespace Lumini.Framework.Tasks.Scheduled
{
    public interface IMergeableJob : IJob, IMergeable
    {
        System.Threading.Tasks.Task CurrentTask { get; }
        System.Threading.Tasks.Task Start();
        void Stop();
    }
}