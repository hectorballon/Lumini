using System;
using Lumini.Concurrent.Enums;

namespace Lumini.Concurrent
{
    public interface IThreadable
    {
        ThreadStatus Status { get; set; }
    }
}
