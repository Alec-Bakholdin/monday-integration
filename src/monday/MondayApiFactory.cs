using System;
using System.Threading;

namespace monday_integration.src.monday
{
    public static class MondayApiFactory
    {
        private static MondayApi _api;
        private static int num_users = 0;

        public static void InitializeApi(string BaseUrl, string ApiToken) {
            Interlocked.Increment(ref num_users);
            _api = _api ?? new MondayApi(BaseUrl, ApiToken);
        }

        public static MondayApi GetApi() {
            if(_api == null) {
                throw new InvalidOperationException("Please call MondayApiFactory.InitializeApi() before MondayApiFactory.GetApi()");
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