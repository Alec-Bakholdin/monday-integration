using System;
using System.Net;
using System.Runtime.Serialization;

namespace monday_integration.src.monday
{
    [Serializable]
    internal class MondayApiException : Exception
    {
        private HttpStatusCode statusCode;
        private string joinedErrors;

        public MondayApiException()
        {
        }

        public MondayApiException(string message) : base(message)
        {
        }

        public MondayApiException(HttpStatusCode statusCode, string joinedErrors) : base($"API failed with status {(int)statusCode}: {joinedErrors}")
        {
            this.statusCode = statusCode;
            this.joinedErrors = joinedErrors;
        }

        public MondayApiException(string message, Exception innerException) : base(message, innerException)
        {
        }

        protected MondayApiException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}