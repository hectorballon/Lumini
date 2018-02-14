using System;

namespace Lumini.Framework.Tasks
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