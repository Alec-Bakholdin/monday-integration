using System;

namespace monday_integration.src.api
{
    public static class AimsApiFactory
    {
        private static AimsApi _api = null;

        public static void InitializeApi(string BaseUrl, string BearerToken) {
            if(_api != null) {
                throw new InvalidOperationException("Api was already initialized, please close the API before initializing");
            }
            _api = new AimsApi(BaseUrl, BearerToken);
        }

        public static AimsApi GetApi() {
            if(_api == null) {
                throw new InvalidOperationException("Please call AimsApiFactory.InitializeApi() before AimsApiFactory.GetApi()");
            }
            return _api;
        }

        public static void CloseApi() {
            if(_api == null) {
                throw new InvalidOperationException("Cannot close API because it is not initialized");
            }
            _api = null;
        }
    }
}