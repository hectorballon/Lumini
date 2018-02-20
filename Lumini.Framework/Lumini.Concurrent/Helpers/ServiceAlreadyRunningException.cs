using System;

namespace Lumini.Concurrent.Helpers
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