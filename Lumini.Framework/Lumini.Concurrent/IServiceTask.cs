using System;
using System.Threading;
using System.Threading.Tasks;
using TaskStatus = Lumini.Concurrent.Enums.TaskStatus;

namespace Lumini.Concurrent
{
    public delegate Task TaskEventHandler(IServiceTask task, EventArgs args);

    public interface IServiceTask
    {
        string Name { get; }
        TaskStatus Status { get; }
        Task Start(CancellationToken token);
        Task Stop();

        event TaskEventHandler BeforeStart;
        event TaskEventHandler Started;
        event TaskEventHandler BeforeStop;
        event TaskEventHandler Stopped;
    }
}