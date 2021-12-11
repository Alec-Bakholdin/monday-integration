using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using RestSharp;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;


namespace AIMS360.Api
{
    public class AimsApiClient
    {
        private string Bearer;
        private static string BaseURL = Environment.GetEnvironmentVariable("Aims360BaseURL");
        private static string RerunBackgroundJobEndpoint = "{{BaseURL}}/jobsmanagement/v1.0/backgroundjob/{{JobID}}/rerun";
        private static string BackgroundJobByJobIDEndpoint = "{{BaseURL}}/jobsmanagement/v1.0/backgroundjob/{{JobID}}";
        private ILogger log;
        public AimsApiClient(string Bearer, ILogger log)
        {
            this.Bearer = Bearer;
            this.log = log;
        }

        /**
         * <summary>
         * Gets aqua job results and groups by the fields set in identifierFields
         * (those where all values match from those fields will be grouped together).
         * {name} is for logging purposes
         * </summary>
         * <param name="jobID">The id of the job to rerun</param>
         * <param name="name">This is used in logging to make sure if we have multiple jobs running we can tell what message comes from where</param>
        */
        
        public async Task<JArray> GetAndProcessJobIDResults(string jobID, string[] identifierFields, string name)
        {
            var jArray = await RerunAquaJobAndGetResults(jobID, name);
            if(identifierFields != null)
                jArray = GroupTogetherJArrayMembers(jArray, identifierFields);

            return jArray;
        }









        // * * * * * * * * * * Aqua Functions * * * * * * * * * * *


        /**
         * <summary>
         * Gets aqua results from AIMS
         * </summary>
         * <param name="jobID">The id of the job to rerun</param>
         * <param name="name">This is used in logging to make sure if we have multiple jobs running we can tell what message comes from where</param>
         */
        public async Task<JArray> RerunAquaJobAndGetResults(string jobID, string name)
        {
            
            var publishLink = await RerunAquaJob(jobID, name);

            log.LogInformation($"{name}: Fetching data from publish link");
            // get from publish link
            var restRequest = new RestRequest();
            restRequest.AddHeader("Authorization", Bearer);
            var restClient = new RestClient(publishLink);
            var restResponse = await restClient.ExecuteAsync(restRequest, Method.GET);

            // check for errors
            if((int)restResponse.StatusCode >= 300)
                throw new Exception($"Error {(int)restResponse.StatusCode} fetching data from publish link: {restResponse.Content}");

            log.LogInformation($"{name}: Successfully fetched data from publish link");
            // otherwise, parse into JObject and return it
            var jobResults = (JObject)JsonConvert.DeserializeObject(restResponse.Content);
            return (JArray)jobResults["data"];
        }

        /**
         * <summary>
         * Calls the rerun endpoint on the job id and polls
         * the API until the job's status is complete, at which point it
         * returns the publish link where we can retrieve the data
         * </summary>
         * <param name="jobID">The id of the job to rerun</param>
         * <param name="name">This is used in logging to make sure if we have multiple jobs running we can tell what message comes from where</param>
         */
        public async Task<string> RerunAquaJob(string jobID, string name)
        {
            log.LogInformation($"{name}: Rerunning Aqua job {jobID}");
            // initialize request and url for rerun
            var rerunRequest = new RestRequest(Method.POST);
            rerunRequest.AddHeader("Authorization", Bearer);
            var rerunURL = RerunBackgroundJobEndpoint.Replace("{{BaseURL}}", BaseURL).Replace("{{JobID}}", jobID);
            

            // make api request, check for http errors
            var rerunResponse = await CallAimsApi(rerunURL, rerunRequest);
            if((int)rerunResponse.StatusCode >= 300)
                throw new Exception($"HTTP Error {(int)rerunResponse.StatusCode} while rerunning background job: {rerunResponse.Content}");

            log.LogInformation($"{name}: Sucessfully reran Aqua job");

            // poll the job until it's completed, up to 100 times
            var pollingURL = BackgroundJobByJobIDEndpoint.Replace("{{BaseURL}}", BaseURL).Replace("{{JobID}}", jobID);
            var pollingRequest = new RestRequest(Method.GET);
            pollingRequest.AddHeader("Authorization", Bearer);

            var failure = 0;
            var limit = 50;
            for(int i = 0; i < limit; i++)
            {
                // give time for job to complete (between 2 and 4 seconds, random to)
                var interval = (new Random().Next()) % 2000 + 2000;
                Thread.Sleep(interval);

                log.LogInformation($"{name}: Polling API - {i+1}/{limit}");
                // make api call, check for errors
                var pollingResponse = await CallAimsApi(pollingURL, pollingRequest);
                if(!pollingResponse.IsSuccessful)
                {
                    var maybe = failure > 3 ? "terminating program" : "retrying one more time";
                    log.LogError($"{name}: Error polling API: {pollingResponse.Content}, {maybe}");
                    if(failure > 3)
                        throw new Exception($"HTTP Error {(int)rerunResponse.StatusCode} while rerunning background job: {pollingResponse.Content}");
                    failure++; // we only throw an exception if two failures in a row
                    continue;
                }

                // conver to json and get the status of the job
                var jobStatusJson = (JObject)JsonConvert.DeserializeObject(pollingResponse.Content);
                var jobStatus = jobStatusJson["jobStatus"].ToString();
                
                // if complete, return the publish link. Otherwise, continue the loop up to 100 times
                if(jobStatus == "Completed")
                    return jobStatusJson["publishLink"].ToString();

                failure = 0;
            }

            // should never get here. If it does, this is an error
            throw new Exception($"Aims job requests timed out. I tried {limit} times");
        }









        // * * * * * * * * * * Grouping Functions * * * * * * * * * * *


        /**
         * Groups together JObject members of the input JArray that share
         * all of the same values for the fields in targetFields.
         * 
         * e.g. {name: "alec", lastname: "bakholdin"},{name: "alec", lastname: "smith"}
         *      gets grouped into {name: "alec", members: [{name: "alec", lastname: "bakholdin}, {...}]}
         */
        private JArray GroupTogetherJArrayMembers(JArray jArray, string[] identifierFields)
        {
            var groupedDictionary = GroupJArrayIntoDictionary(jArray, identifierFields);

            var groupedJArray = new JArray();
            foreach(KeyValuePair<string, JObject> pair in groupedDictionary)
                groupedJArray.Add(pair.Value);

            return groupedJArray;
        }

        /**
         * Part 1 of GroupTogetherJArrayMembers: put all related objects into
         * a single dictionary where the key is the concatenation of all the 
         * identifierFields' values in the jArray members
         */

        public Dictionary<string, JObject> GroupJArrayIntoDictionary(JArray jArray, string[] identifierFields)
        {
            var groupedDictionary = new Dictionary<string, JObject>();

            // group the elments
            foreach(JObject jObject in jArray)
            {
                // get corresponding values for identifierFields
                var identifierValues = GetIdentifierValues(jObject, identifierFields);

                // create key out of those elements by joining them together with ' '
                var valueStringArray = identifierValues
                                        .Select (value => value == null ? "" : value.ToString())
                                        .ToArray();
                var key = String.Join(' ', valueStringArray);

                // create group JObject if it doesn't exist yet
                if(!groupedDictionary.ContainsKey(key))
                    groupedDictionary[key] = InitGroupedJObject(identifierFields, identifierValues);
                
                // add the jObject to its group
                var groupedJObjectMembers = (JArray)groupedDictionary[key]["members"];
                groupedJObjectMembers.Add(jObject);
            }

            return groupedDictionary;
        }   



        /**
         * Gets the corresponding values in the jObject for the
         * fields in identifierFields. If a value isn't present, 
         * sets the value to null to preserve equal length
         */
        private JToken[] GetIdentifierValues(JObject jObject, string[] identifierFields)
        {
            var identifierValues = new JToken[identifierFields.Length];
            
            for(int i = 0; i < identifierFields.Length; i++)
            {
                var identifierField = identifierFields[i];
                if(jObject.ContainsKey(identifierField))
                    identifierValues[i] = jObject[identifierField];
            }

            return identifierValues;
        }

        /**
         * Initialize the empty JObject that groups together other JObjects
         */
        private JObject InitGroupedJObject(string[] identifierFields, JToken[] identifierValues)
        {
            var jObject = new JObject();

            // add the identifier fields to the jObject as field/value pairs
            for(int i = 0; i < identifierFields.Length; i++)
                jObject.Add(identifierFields[i], identifierValues[i]);
            
            // add empty jArray for members to be added next
            jObject.Add("members", new JArray());

            return jObject;
        }













        // * * * * * * * * * * Helper Functions * * * * * * * * *

        /**
        * Simplest Api request, gets the json from the get request and
        * converts it to JObject using JsonConvert.DeserializeObject.
        * Implements exponential backoff. Does not do error checking beyond
        * the weird 400 request.
        */
        private async Task<IRestResponse> CallAimsApi(string url, RestRequest request)
        {
            // initialize everything
            var client = new RestClient(url);

            // call the request
            IRestResponse response = null;
            int counter = 0;
            do{
                // exponential backoff, after the first iteration
                if(counter > 0)
                    Thread.Sleep((int)Math.Pow(2, counter - 1) * 1000);

                // get api response
                response = await client.ExecuteAsync(request);

                // handle the case where the request fails due to overloaded servers
                if((int)response.StatusCode == 400)
                {
                    var responseObj = (JObject)JsonConvert.DeserializeObject(response.Content);
                    if(!responseObj.ContainsKey("error"))
                    {
                        //log.LogError(response.Content); this was to handle the inconsistent error message, but we now handle that elsewhere
                        break;
                    }
                    if(responseObj["error"]["code"].ToString() != "Request_ProcessingFailed")
                        break;
                }
                // if some well-defined error or OK, we exit out of the loop
                else
                    break;

            }while(counter++ < 5);

            return response;
        }
    
    }
}