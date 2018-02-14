using System;
using System.Collections.Generic;

namespace Lumini.Framework.Dataflow
{
    public class DataSourceEventArgs<T> : EventArgs
        where T : class
    {
        public IEnumerable<T> Items { get; internal set; }
    }
}