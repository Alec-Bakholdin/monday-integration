using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using monday_integration.src.api;
using monday_integration.src.aqua;
using monday_integration.src.aqua.model;
using monday_integration.src.logging;
using monday_integration.src.monday;
using monday_integration.src.monday.model;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace monday_integration.src
{
    public static class Main
    {
        private static MondayIntegrationSettings settings;
        private static AimsLogger logger;

        public static async Task SyncMonday(ILogger logger) {
            Initialize(logger);

            try{
                await Execute();
            }catch(Exception) {
                throw;
            }finally{
                Cleanup();
            }
        }

        private static async Task Execute() {
            var stylePOsAquaClient = new AquaClient(settings.AimsStylePOsLineDetailsAndFieldsJobId);
            var allocationReportAquaClient = new AquaClient(settings.AimsAllocationDetailsReportJobId);
            //await aquaClient.RerunBackgroundJob();
            var vendorPOs = await stylePOsAquaClient.FetchData<WitreStyleVendorPO>();

            var allocationReport = await allocationReportAquaClient.FetchData<WitreAllocationDetails>();
            var groupedAllocationReport = allocationReport
                .Where(obj => obj.RecordType == RecordType.PO)
                .GroupBy(obj => obj.GetIdentifier())
                .Select(group => {
                    var elementCopy = group.First().ShallowCopy();
                    elementCopy.Allocatedqty = group.Sum(item => item.Allocatedqty);
                    return elementCopy;
                })
                .ToList();
            var stylePOs = vendorPOs.Select(vendorPO => new WitreStylePO(vendorPO, 
                groupedAllocationReport.Where(orderLine => 
                    orderLine.WIPReference.Trim() == vendorPO.PurchaseOrderNo.Trim() &&
                    orderLine.Style.Trim() == vendorPO.Style.Trim() &&
                    orderLine.Color.Trim() == vendorPO.Color.Trim()
                )
            )).ToList();
            

            var mondayClient = new MondayClient();
            var aimsIntegrationBoard = await mondayClient.GetMondayBoard(settings.MondayAimsIntegrationBoardId);
            var integratedPOs = aimsIntegrationBoard.items.Select(item => item.name).ToHashSet();
            var mondayItems = mondayClient.MapWitreStylePosToMondayItems(stylePOs, settings.MondayAimsIntegrationBoardId);
            foreach(var mondayItem in mondayItems) {
                if(!integratedPOs.Contains(mondayItem.name)) {
                    await mondayClient.CreateMondayItem(mondayItem);
                }
            }

            //var mondayItems = mondayClient.MapWitreStylePosToMondayItems(stylePOs, settings.MondayAimsIntegrationBoardId);
            //mondayItems = mondayItems.Where(item => item.board_id != null).ToList();
            //foreach(var mondayItem in mondayItems) {
            //    if(!integratedPOs.Contains(mondayItem.name)) {
            //        await mondayClient.CreateMondayItem(mondayItem);
            //    }
            //}
        }



        private static void Initialize(ILogger logger) {
            AimsLoggerFactory.logger = logger;
            settings = new MondayIntegrationSettings(Environment.GetEnvironmentVariables());
            AimsApiFactory.InitializeApi(settings.Aims360BaseURL, settings.AimsBearerToken);
            MondayApiFactory.InitializeApi(settings.MondayBaseURL, settings.MondayApiKey);
            Main.logger = AimsLoggerFactory.CreateLogger(typeof(Main));
        }

        private static void Cleanup() {
            AimsApiFactory.CloseApi();
            MondayApiFactory.CloseApi();
        }
    }
}