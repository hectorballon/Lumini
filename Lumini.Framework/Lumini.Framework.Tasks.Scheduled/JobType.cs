using System;

namespace Lumini.Framework.Tasks.Scheduled
{
    [Flags]
    public enum JobType
    {
        OnDemand = 1,
        Scheduled = 2
    }
}