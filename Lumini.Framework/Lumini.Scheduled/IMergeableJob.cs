namespace Lumini.Scheduled
{
    public interface IMergeableJob : IJob, IMergeable
    {
        System.Threading.Tasks.Task CurrentTask { get; }
        System.Threading.Tasks.Task Start();
        void Stop();
    }
}