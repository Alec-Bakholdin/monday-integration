using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using GraphQL;
using GraphQL.Client;
using GraphQL.Client.Http;
using System.Threading;

using AIMS360.Api;
using AIMS360.Monday;
using AIMS360.Azure;
using Custom.Utility;

namespace AIMS360.Main
{
    using static MondayUtility;
    using static JsonUtility;
    public static class HttpTriggerCSharp1
    {
        private static string TableConnectionString     = Environment.GetEnvironmentVariable("TableConnectionString");
        private static string TableDefaultPartitionKey  = Environment.GetEnvironmentVariable("TableDefaultPartitionKey");
        private static string TableDefaultEntityKey     = Environment.GetEnvironmentVariable("TableDefaultEntityKey");

        private static string AimsBearerToken           = Environment.GetEnvironmentVariable("AimsBearerToken");
        private static string OpenStylePOsJobID         = Environment.GetEnvironmentVariable("OpenStylePOsJobID");
        private static string OpenMaterialPOsJobID      = Environment.GetEnvironmentVariable("OpenMaterialPOsJobID");
        private static string CutTicketsJobId           = Environment.GetEnvironmentVariable("CutTicketsJobId");
        private static string TableName                 = Environment.GetEnvironmentVariable("TableName");
        private static string MondayTemplateId          = Environment.GetEnvironmentVariable("MondayTemplateId");

        [FunctionName("ManualTrigger")]
        public static async Task<IActionResult> RunManualTrigger(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post", Route = null)] HttpRequest req,
            ILogger log)
        {
            try{
                //TODO: uncomment
                //await Execute(log);
            }catch(Exception e){
                log.LogError(e.Message);
                return new BadRequestObjectResult("There was an error updating Monday. Please contact the programmer.");
            }

            return new OkObjectResult("Finished Updating Monday Data");
        }

        [FunctionName("TimerTrigger")]
        public static async Task RunTimerTrigger(
            [TimerTrigger("0 0 9 * * *")] TimerInfo timerInfo,
            ILogger log
        )
        {
            //TODO: uncomment
            //await Execute(log);
        }

        public static async Task test(ILogger log)
        {
            for(int i = 0; i < 5; i++)
            {
                Thread.Sleep(3000);
                log.LogInformation($"{i}");
            }
        }

        public static async Task Execute(ILogger log)
        {
            MondayUtility.log = log;


            var AimsAzureTable = new AzureTable(TableConnectionString, TableName, TableDefaultPartitionKey, TableDefaultEntityKey, log);
            var AimsApi = new AimsApiClient(AimsBearerToken, log);

            var taskList = new List<Task<JArray>>();
            var openPOsFields = new string[]{"PurchaseOrderNo", "IssuedDate", "PO Cancel", "Vendor", "Name", "XFactory", "Start Date", "XRef", "POType"};
            var cutTicketsFields = new string[]{"Ticket", "Entered", "Complete", "Status"};

            taskList.Add(AimsApi.GetAndProcessJobIDResults(OpenStylePOsJobID, openPOsFields, "Style POs"));
            taskList.Add(AimsApi.GetAndProcessJobIDResults(OpenMaterialPOsJobID, openPOsFields, "Material POs"));
            taskList.Add(AimsApi.GetAndProcessJobIDResults(CutTicketsJobId, cutTicketsFields, "Cut Tickets"));

            //var apiClient = new ApiClient(AimsBearerToken);
            
            // wait on aims tasks and get results
            Task.WaitAll(taskList.ToArray());

            var openStylePOsArray    = await taskList[0];
            var openMaterialPOsArray = await taskList[1];
            var cutTicketsArray      = await taskList[2];

            var openStylePOsByVendor = AssociateJArray(openStylePOsArray, "Vendor");
            var openMaterialPOsByVendor = AssociateJArray(openMaterialPOsArray, "Vendor");


            // sync POs
            await SyncStylePOs(openStylePOsByVendor, AimsAzureTable);
            await SyncMaterialPOs(openMaterialPOsByVendor, AimsAzureTable);

            // sync Cut Tickets
            await SyncBoardWithJArray(
                cutTicketsArray,
                "Cut Tickets",
                AimsAzureTable,
                null,
                null,
                Item_GetCutTicketColumnValuesAsStr,
                Subitem_GetCutTicketColumnValuesAsStr
            );
        }

        /**
         * <summary>
         * Syncs the information from the StylePOs dictionary with the data in Monday
         * </summary>
         * <param name="StylePOs">The dictionary that's generated from AssociateJArray</param>
         * <param name="AimsAzureTable">The table where all the identifying information is stored</param>
         * <returns>Always returns true, return value necessary for async</returns>
         */
        private static async Task SyncStylePOs(Dictionary<string, JArray> StylePOs, AzureTable AimsAzureTable)
        {
            foreach(KeyValuePair<string, JArray> pair in StylePOs)
            {
                // fetch values from pair
                var POArray = pair.Value;
                var CompanyName = pair.Key;
                var CompanyDescription = (string)POArray[0]["Name"]; // each element is guaranteed to have the same name since it's the same company. Even if it's not, not a big deal

                // call sync board function
                await SyncBoardWithJArray(
                    POArray,                                // AimsEntries
                    CompanyName,                            // BoardName
                    AimsAzureTable,                         // AimsAzureTable
                    CompanyDescription,                     // BoardDescription
                    MondayTemplateId,                       // MondayBoardTemplateId
                    Item_GetPOColumnValuesAsStr,            // GetItemInformation (Function)
                    Subitem_GetStylePOColumnValuesAsStr     // GetSubitemInformation (Function)
                );
            }
        }


        /**
         * <summary>
         * Syncs the information from the MaterialPOs dictionary with the data in Monday
         * </summary>
         * <param name="MaterialPOs">The dictionary that's generated from AssociateJArray</param>
         * <param name="AimsAzureTable">The table where all the identifying information is stored</param>
         * <returns>Always returns true, return value necessary for async</returns>
         */
        private static async Task SyncMaterialPOs(Dictionary<string, JArray> MaterialPOs, AzureTable AimsAzureTable)
        {
            foreach(KeyValuePair<string, JArray> pair in MaterialPOs)
            {
                // fetch values from pair
                var POArray = pair.Value;
                var CompanyName = pair.Key;
                var CompanyDescription = (string)POArray[0]["Name"]; // each element is guaranteed to have the same name since it's the same company. Even if it's not, not a big deal

                // call sync board function
                await SyncBoardWithJArray(
                    POArray,                                // AimsEntries
                    CompanyName,                            // BoardName
                    AimsAzureTable,                         // AimsAzureTable
                    CompanyDescription,                     // BoardDescription
                    MondayTemplateId,                       // MondayBoardTemplateId
                    Item_GetPOColumnValuesAsStr,            // GetItemInformation (Function)
                    Subitem_GetMaterialPOColumnValuesAsStr  // GetSubitemInformation (Function)
                );
            }
        }
    }
}
