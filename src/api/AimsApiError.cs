using System;
using System.Net;
using Newtonsoft.Json;

namespace monday_integration.src.api
{
    [Serializable]
    internal class AimsApiError : Exception
    {
        public string requestId {get; private set;}
        public AimsApiErrorContainer error {get; private set;}
        public HttpStatusCode statusCode {get; private set;}
        
        public AimsApiError(string responseContent, HttpStatusCode statusCode) : base(responseContent)
        {
            dynamic apiError = JsonConvert.DeserializeObject(responseContent);
            this.requestId = apiError.requestId;
            this.error = new AimsApiErrorContainer(apiError.error.code.ToString(), apiError.error.message.ToString());
            this.statusCode = statusCode;
        }

        public override string ToString()
        {
            return JsonConvert.SerializeObject(this);
        }
    }
}