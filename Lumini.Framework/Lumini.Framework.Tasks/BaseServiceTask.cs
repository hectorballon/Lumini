using System;
using System.IO;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Lumini.Framework.Tasks
{
    public abstract class BaseServiceTask : IServiceTask
    {
        private ManualResetEvent _runSyncEvent;

        protected BaseServiceTask(string name)
        {
            //Thread.CurrentThread.Name = $"{name}.{DateTime.UtcNow:yyyy-MM-ddTHH:mm:ss}.Thread";
            Name = name;
            Status = TaskStatus.NotStarted;
        }

        public string Name { get; }

        public virtual async Task Start(CancellationToken token)
        {
            if (token.IsCancellationRequested) return;

            lock (new object())
            {
                CheckStatusIsValidBeforeStarting();
                TaskScheduler.UnobservedTaskException += DumpUnhandledTaskExceptions;
                Status = TaskStatus.Starting;
            }

            if (BeforeStart != null) await BeforeStart(this, EventArgs.Empty);
            await StartTask(token);
        }

        public virtual async Task Stop()
        {
            lock (new object())
            {
                CheckStatusIsValidBeforeStopping();
                Status = TaskStatus.Stopping;
            }
            if (BeforeStop != null) await BeforeStop(this, EventArgs.Empty);
            _runSyncEvent?.Set();
            Status = TaskStatus.Stopped;
            if (Stopped != null) await Stopped(this, EventArgs.Empty);
        }

        public TaskStatus Status { get; protected set; }

        public event TaskEventHandler BeforeStart;
        public event TaskEventHandler Started;
        public event TaskEventHandler BeforeStop;
        public event TaskEventHandler Stopped;

        private void CheckStatusIsValidBeforeStarting()
        {
            switch (Status)
            {
                case TaskStatus.Starting:
                    throw new ServiceAlreadyStartingException();
                case TaskStatus.Running:
                    throw new ServiceAlreadyRunningException();
                case TaskStatus.NotStarted:
                case TaskStatus.Stopped:
                    break;
                default:
                    throw new InvalidOperationException(
                        "You can only start a service with NotStarted or Stopped status");
            }
        }

        private void CheckStatusIsValidBeforeStopping()
        {
            if (Status == TaskStatus.Stopping)
                throw new ServiceAlreadyStoppingException();
            if (Status != TaskStatus.Running && Status != TaskStatus.Idle)
                throw new InvalidOperationException("You can only stop a service with Running or Idle status");
        }

        protected virtual async Task StartTask(CancellationToken token)
        {
            using (_runSyncEvent = new ManualResetEvent(false))
            {
                var cancellationTokenSrc = await RunTask();
                Status = TaskStatus.Running;
                if (Started != null) await Started(this, EventArgs.Empty);
                WaitHandle.WaitAny(
                    new[] {token.WaitHandle, _runSyncEvent});
                cancellationTokenSrc.Cancel();
                await Stop();
                if (!token.IsCancellationRequested)
                    _runSyncEvent.Reset();
            }
        }

        private async Task<CancellationTokenSource> RunTask()
        {
            var cancellationTokenSrc = new CancellationTokenSource();
            await Task.Factory.StartNew(async () => { await DoWork(cancellationTokenSrc.Token); },
                    cancellationTokenSrc.Token)
                .ConfigureAwait(false);
            return cancellationTokenSrc;
        }

        private void DumpUnhandledTaskExceptions(object sender, UnobservedTaskExceptionEventArgs args)
        {
            var path = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            path = Path.Combine(path, "errorlog");

            if (!Directory.Exists(path))
                Directory.CreateDirectory(path);

            var text = new StringBuilder();
            foreach (var ex in args.Exception.Flatten().InnerExceptions)
            {
                OnUnhandledExceptionThrown(ex);
                text.Append(ex);
                foreach (var key in ex.Data.Keys)
                    text.Append(string.Concat("Inner - ", key, ex.Data[key].ToString()));
                text.AppendLine();
            }
            File.WriteAllText(
                Path.Combine(path, string.Format("{0}.txt", DateTime.UtcNow.ToString("yyyyMMdd_HHmmss.ffff"))),
                text.ToString());

            args.SetObserved();
        }

        protected virtual void OnUnhandledExceptionThrown(Exception ex)
        {
        }

        protected abstract Task DoWork(CancellationToken token);
    }
}