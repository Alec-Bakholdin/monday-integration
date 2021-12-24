using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using GraphQL;
using GraphQL.Client.Http;
using GraphQL.Client.Serializer.Newtonsoft;
using monday_integration.src.monday.model;
using Newtonsoft.Json;
using RateLimiter;

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

        public async Task<List<MondayBoard>> GetMondayBoards(params string[] options) {
            var query = @"{boards(){$query_options}}";
            var variables = new {
                query_options = String.Join(", ", options)
            };
            
            var request = new GraphQLRequest() {Query = SubstituteVariables(query, variables)};

            var response = await QueryAsync<MondayBoardList>(request);
            return response.boards;
        }

        private async Task<T> MutateAsync<T>(GraphQLRequest request) {
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

        private async Task<T> QueryAsync<T>(GraphQLRequest request) {
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

        private string SubstituteVariables(string templateString, object variables) {
            var outputString = templateString;
            foreach(var property in variables.GetType().GetProperties()) {
                var varName = "$" + property.Name;
                var varValue = property.GetValue(variables).ToString();
                outputString = outputString.Replace(varName, varValue);
            }
            return outputString;
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