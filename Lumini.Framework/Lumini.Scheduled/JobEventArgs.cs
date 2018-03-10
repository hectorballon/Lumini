using System;

namespace Lumini.Scheduled
{
    public class JobEventArgs : EventArgs
    {
        public JobExecutionContext Context { get; internal set; }
    }
}