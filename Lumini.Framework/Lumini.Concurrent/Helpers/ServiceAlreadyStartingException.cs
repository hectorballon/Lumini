using System;

namespace Lumini.Concurrent.Helpers
{
    public class ServiceAlreadyStartingException : Exception
    {
        public ServiceAlreadyStartingException()
        {
        }

        public ServiceAlreadyStartingException(string message)
            : base(message)
        {
        }
    }
}