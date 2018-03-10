using System;
using System.Threading;
using System.Threading.Tasks;
using Lumini.Common;

namespace Lumini.Concurrent.Tasks
{
    public class BasicServiceTask : BaseServiceTask
    {
        public BasicServiceTask(IServiceConfiguration configuration, ILogger logger)
            : base(configuration)
        {
            Logger = logger;
        }

        protected ILogger Logger { get; }

        public Func<Task> Execute { get; set; }

        protected override async Task DoWork(CancellationToken token)
        {
            if (Execute == null) throw new ArgumentNullException($"Method {nameof(BasicServiceTask)}->{nameof(Execute)} was not defined");
            token.Register(async () => await Stop());
            while (!token.IsCancellationRequested)
            {
                if (!Settings.Enabled)
                {
                    Status = Enums.TaskStatus.Idle;
                    await Task.Delay(TimeSpan.FromDays(1).Milliseconds, token);
                    continue;
                }
                Status = Enums.TaskStatus.Running;
                await Execute();
                await Task.Delay(Settings.IdleTimeInMilliseconds, token);
            }
        }
    }
}