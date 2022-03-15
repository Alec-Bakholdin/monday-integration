using RestSharp;
using RateLimiter;
using ComposableAsync;
using System;
using System.Threading.Tasks;
using monday_integration.src.logging;
using System.Collections.Generic;
using System.Threading;

namespace monday_integration.src.api
{
    public class AimsApi
    {
        private AimsLogger logger;
        private RestClient _restClient;
        private TimeLimiter _timeLimiter;
        private SemaphoreSlim _cacheLock;
        private Dictionary<string, object> _cache;

        public AimsApi(string baseUrl, string bearerToken) {
            InitializeApi(baseUrl, bearerToken, 1);
        }
        
        public AimsApi(string baseUrl, string bearerToken, int requestsPerSecond) {
            InitializeApi(baseUrl, bearerToken, requestsPerSecond);
        }

        private void InitializeApi(String baseUrl, String bearerToken, int requestsPerSecond) {
            this.logger = AimsLoggerFactory.CreateLogger(typeof(AimsApi));
            this._restClient = new RestClient(baseUrl);
            this._restClient.AddHandler("text/csv", () => { return new CsvDeserializer();});
            this._restClient.AddDefaultHeader("Authorization", bearerToken);
            this._timeLimiter = TimeLimiter.GetFromMaxCountByInterval(requestsPerSecond, TimeSpan.FromSeconds(1));
            this._cacheLock = new SemaphoreSlim(1, 1);
            this._cache = new Dictionary<string, object>();
        }

        public async Task<T> GetCachedResponseAsync<T>(string resource) {
            await _cacheLock.WaitAsync();
            if(_cache.ContainsKey(resource)) {
                _cacheLock.Release();
                return (T)_cache[resource];
            }
            var response = await GetAsync<T>(resource);
            _cache[resource] = response;
            _cacheLock.Release();
            return response;
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