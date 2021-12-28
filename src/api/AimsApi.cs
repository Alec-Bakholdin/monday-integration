using RestSharp;
using RateLimiter;
using ComposableAsync;
using System;
using System.Threading.Tasks;
using monday_integration.src.logging;

namespace monday_integration.src.api
{
    public class AimsApi
    {
        private AimsLogger logger;
        private RestClient _restClient;
        private TimeLimiter _timeLimiter;

        public AimsApi(string baseUrl, string bearerToken) {
            this.logger = AimsLoggerFactory.CreateLogger(typeof(AimsApi));
            this._restClient = new RestClient(baseUrl);
            this._restClient.AddDefaultHeader("Authorization", bearerToken);
            this._timeLimiter = TimeLimiter.GetFromMaxCountByInterval(1, TimeSpan.FromSeconds(1));
        }
        
        public AimsApi(string baseUrl, string bearerToken, int requestsPerSecond) {
            this.logger = AimsLoggerFactory.CreateLogger(typeof(AimsApi));
            this._restClient = new RestClient(baseUrl);
            this._restClient.AddDefaultHeader("Authorization", bearerToken);
            this._timeLimiter = TimeLimiter.GetFromMaxCountByInterval(requestsPerSecond, TimeSpan.FromSeconds(1));
        }

        public async Task<T> GetAsync<T>(string resource) {
            var restRequest = new RestRequest(resource, Method.GET);
            var response = await ExecuteAsync<T>(restRequest);
            if(response.IsSuccessful) {
                return response.Data;
            }
            throw new AimsApiError(response.Content, response.StatusCode);
        }

        public async Task<T> PostAsync<T>(string resource, object body) {
            var restRequest = new RestRequest(resource, Method.POST);
            restRequest.AddJsonBody(body);
            var response = await ExecuteAsync<T>(restRequest);
            if(response.IsSuccessful) {
                return response.Data;
            }
            throw new AimsApiError(response.Content, response.StatusCode);
        }

        public async Task<IRestResponse<T>> ExecuteAsync<T>(IRestRequest restRequest){
            await _timeLimiter;
            restRequest.IncreaseNumAttempts();
            restRequest.IncreaseNumAttempts();
            logger.Debug($"Executing {restRequest.Method} {_restClient.BaseUrl}{restRequest.Resource}{restRequest.Parameters}");
            var response = await _restClient.ExecuteAsync<T>(restRequest);
            if(!response.IsSuccessful) {
                throw new AimsApiError(response.Content, response.StatusCode);
            }
            return response;
        }
    }
}