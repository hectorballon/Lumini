﻿using System;

namespace Lumini.Framework.Tasks.Scheduled
{
    public interface ISchedule : IEquatable<ISchedule>
    {
        string Name { get; }
        string Year { get; }
        string Month { get; }
        string Hours { get; }
        string Minutes { get; }
        string DayOfMonth { get; }
        string DayOfWeek { get; }
    }
}