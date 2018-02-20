using System;

namespace Lumini.Concurrent.Helpers
{
    public class ServiceAlreadyStoppingException : Exception
    {
        public ServiceAlreadyStoppingException()
        {
        }

        public ServiceAlreadyStoppingException(string message)
            : base(message)
        {
        }
    }
}