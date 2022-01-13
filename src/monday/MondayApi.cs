using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using GraphQL;
using GraphQL.Client.Http;
using GraphQL.Client.Serializer.Newtonsoft;

namespace monday_integration.src.monday
{
    public class MondayApi
    {
        private GraphQLHttpClient graphQLClient;
        private SemaphoreSlim __lock;

        public MondayApi(string BaseUrl, string ApiToken) {
            graphQLClient = new GraphQLHttpClient(BaseUrl, new NewtonsoftJsonSerializer());
            graphQLClient.HttpClient.DefaultRequestHeaders.Add("Authorization", ApiToken);

            __lock = new SemaphoreSlim(1, 1);
        }

        public async Task<T> MutateAsync<T>(GraphQLRequest request) {
            try{
                while(true) {
                    await __lock.WaitAsync();
                    var response = await graphQLClient.SendMutationAsync<T>(request);
                    if(await IsSuccessfulResponse(response)) {
                        return response.Data;
                    }
                }
            }catch(Exception) {
                throw;
            } finally {
                __lock.Release();
            }
        }

        public async Task<T> QueryAsync<T>(GraphQLRequest request) {
            try{
                while(true) {
                    await __lock.WaitAsync();
                    var response = await graphQLClient.SendQueryAsync<T>(request);
                    if(await IsSuccessfulResponse(response)) {
                        return response.Data;
                    }
                }
            }catch(Exception) {
                throw;
            } finally {
                __lock.Release();
            }
        }

        private async Task<bool> IsSuccessfulResponse<T>(GraphQLResponse<T> response) {
            const int FailDelayInMs = 10*1000;
            if(EncounteredUnexpectedError(response.Errors)) {
                var httpResponse = response.AsGraphQLHttpResponse();
                var joinedErrors = String.Join("; ", response.Errors.Select(r => r.Message));
                throw new MondayApiException(httpResponse.StatusCode, joinedErrors);
            } 
            else if(ComplexityLimitReached(response.Errors)) {
                await Task.Delay(FailDelayInMs);
                return false;
            }
            return true;
        }


        private bool EncounteredUnexpectedError(GraphQLError[] graphQLErrors) {
            return graphQLErrors != null && graphQLErrors.Length > 0 && !ComplexityLimitReached(graphQLErrors);
        }

        private bool ComplexityLimitReached(IEnumerable<GraphQLError> graphQLErrors) {
            const string ComplexityLimitRegex = @"Complexity budget exhausted, query cost \d+ budget remaining \d+ out of \d+ reset in \d+ seconds";
            if(graphQLErrors == null) {
                return false;
            }
            foreach(var error in graphQLErrors) {
                var matches = Regex.Match(error.Message, ComplexityLimitRegex);
                if(matches.Success) {
                    return true;
                }
            }
            return false;
        }
    }
}