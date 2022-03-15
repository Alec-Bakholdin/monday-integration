using System;
using System.Threading;

namespace monday_integration.src.api
{
    public static class AimsApiFactory
    {
        private static AimsApi _api = null;
        private static int num_users = 0;

        public static void InitializeApi(string BaseUrl, string BearerToken) {
            Interlocked.Increment(ref num_users);
            _api = _api ?? new AimsApi(BaseUrl, BearerToken, 5);
        }

        public static AimsApi GetApi() {
            if(_api == null) {
                throw new InvalidOperationException("Please call AimsApiFactory.InitializeApi() before AimsApiFactory.GetApi()");
            }
            return _api;
        }

        public static void CloseApi() {
            if(Interlocked.Decrement(ref num_users) == 0) {
                _api = null;
            }
        }
    }
}