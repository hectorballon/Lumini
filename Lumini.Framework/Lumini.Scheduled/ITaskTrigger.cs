namespace Lumini.Scheduled
{
    public delegate System.Threading.Tasks.Task TaskTriggerEventHandler(ITaskTrigger job, JobEventArgs args);

    public interface ITaskTrigger
    {
        JobExecutionContext Context { get; }
        event TaskTriggerEventHandler BeforeExecution;
        event TaskTriggerEventHandler ExecutionCompleted;
    }
}