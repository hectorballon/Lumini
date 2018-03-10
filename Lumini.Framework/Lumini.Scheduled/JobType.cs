using System;

namespace Lumini.Scheduled
{
    [Flags]
    public enum JobType
    {
        OnDemand = 1,
        Scheduled = 2
    }
}