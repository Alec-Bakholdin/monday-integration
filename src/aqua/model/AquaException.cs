using System;
using System.Runtime.Serialization;

namespace monday_integration.src.aqua
{
    [Serializable]
    internal class AquaException : Exception
    {
        public AquaException()
        {
        }

        public AquaException(string message) : base(message)
        {
        }

        public AquaException(string message, Exception innerException) : base(message, innerException)
        {
        }

        protected AquaException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}