using System;

namespace Lumini.Framework.Tasks.Scheduled
{
    [Flags]
    public enum TaskResult
    {
        Success = 1,
        Fail = 2,
        ValidationFailed = 4,
        SuccessWithWarnings = 8,
        NoData = 16,
        Aborted = 32,
        NotExecuted = 64
    }
}