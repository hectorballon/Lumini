using System;
using System.Collections.Generic;

namespace Lumini.Framework.Tasks.Scheduled
{
    public interface IJob : IEquatable<IJob>
    {
        Guid Id { get; }
        DateTime? LastExecution { get; }
        DateTime? NextExecution { get; }
        bool IsRunning { get; }
        bool Enabled { get; }
        string Name { get; }
        string Description { get; }
        TaskResult LastResult { get; }
        bool ForceExecution { get; set; }
        JobPriority Priority { get; }
        JobType Type { get; }
        Task Task { get; set; }
        IList<ISchedule> Schedules { get; }
    }
}