using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using monday_integration.src.api;
using monday_integration.src.aqua.model;
using monday_integration.src.logging;
using monday_integration.src.monday;
using monday_integration.src.monday.model;
using withered_tree_monday_integration.src;

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
            var mainData = new MainDataFetcher(settings);
            await mainData.FetchAllInParallel();

            var allocationReportGroupedByVendorPO = mainData.allocationDetails
                .Where(obj => obj.RecordType == RecordType.PO)
                .GroupBy(obj => obj.GetIdentifier())
                .Select(group => {
                    var elementCopy = group.First().ShallowCopy();
                    elementCopy.Allocatedqty = group.Sum(item => item.Allocatedqty);
                    return elementCopy;
                })
                .GroupBy(obj => obj.WIPReference)
                .ToDictionary(group => group.Key, group => group.ToList());
            mainData.vendorPOs   
                    .Where(po => allocationReportGroupedByVendorPO.ContainsKey(po.PurchaseOrderNo))
                    .ToList()
                    .ForEach(po => po.allocationDetails = allocationReportGroupedByVendorPO[po.PurchaseOrderNo]);
            
            var mondayClient = new MondayClient();
            var integratedPOs = mainData.aimsIntegrationBoard.items.ToDictionary(item => item.name);
            var mondayItems = MondayClient.MapWitreStylePosToMondayItems(mainData.vendorPOs, settings.MondayAimsIntegrationBoardId);
            foreach(var mondayItem in mondayItems) {
                MondayItem oldItem;
                if(!integratedPOs.TryGetValue(mondayItem.name, out oldItem)) {
                    await mondayClient.CreateMondayItem(mondayItem);
                } else if(mondayItem.isDifferentFromOldItem(oldItem)){
                    await mondayClient.UpdateMondayItem(oldItem, mondayItem);
                    logger.Info($"Updating {oldItem.name}({oldItem.id})");
                }
            }
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