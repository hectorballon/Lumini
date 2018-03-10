namespace Lumini.Concurrent
{
    public interface IServiceConfiguration : IService, IClassConfiguration
    {
        int IdleTimeInMilliseconds { get; }
        int NumberOfWorkers { get; }
        int WorkerBatchSize { get; }
        int PropagationDelayInMilliseconds { get; }
    }
}