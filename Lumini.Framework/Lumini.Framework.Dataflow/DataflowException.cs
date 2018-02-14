using System;
using System.Runtime.Serialization;

namespace Lumini.Framework.Dataflow
{
    public class DataflowException : Exception
    {
        public DataflowException()
        {
        }

        public DataflowException(string message) : base(message)
        {
        }

        public DataflowException(string message, Exception inner) : base(message, inner)
        {
        }

        protected DataflowException(
            SerializationInfo info,
            StreamingContext context)
            : base(info, context)
        {
        }
    }
}