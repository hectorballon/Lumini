using System;

namespace Lumini.Framework.Tasks
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