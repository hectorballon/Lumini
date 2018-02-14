using System;

namespace Lumini.Framework.Tasks
{
    public class ServiceAlreadyRunningException : Exception
    {
        public ServiceAlreadyRunningException()
        {
        }

        public ServiceAlreadyRunningException(string message)
            : base(message)
        {
        }
    }
}