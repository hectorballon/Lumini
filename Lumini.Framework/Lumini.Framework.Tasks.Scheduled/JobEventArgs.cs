using System;

namespace Lumini.Framework.Tasks.Scheduled
{
    public class JobEventArgs : EventArgs
    {
        public JobExecutionContext Context { get; internal set; }
    }
}