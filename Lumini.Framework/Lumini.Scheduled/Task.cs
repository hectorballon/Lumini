﻿using System.Collections.Generic;

namespace Lumini.Scheduled
{
    public abstract class Task
    {
        public virtual bool CanBeExecuted()
        {
            return true;
        }

        public abstract TaskResult Run(string jobName, string scheduleName, IDictionary<string, object> parameters);
    }
}