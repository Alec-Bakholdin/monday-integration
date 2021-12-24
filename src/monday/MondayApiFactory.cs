using System;

namespace monday_integration.src.monday
{
    public static class MondayApiFactory
    {
        private static MondayApi _api;

        public static void InitializeApi(string BaseUrl, string ApiToken) {
            if(_api != null) {
                throw new InvalidOperationException("Api was already initialized, please close the API before initializing");
            }
            _api = new MondayApi(BaseUrl, ApiToken);
        }

        public static MondayApi GetApi() {
            if(_api == null) {
                throw new InvalidOperationException("Please call MondayApiFactory.InitializeApi() before MondayApiFactory.GetApi()");
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