using System;
using System.Collections.Generic;
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

        private static async Task Execute()
        {
            var mainData = new MainDataFetcher(settings);
            await mainData.FetchAllInParallel();

            Dictionary<string, List<WitreAllocationDetails>> allocationReportGroupedByVendorPO = GroupAllocationDetails(mainData);
            AddAllocationDetailsToVendorPOs(mainData, allocationReportGroupedByVendorPO);
            List<MondayItem> mondayItems = ConvertVendorPOsToMondayItems(mainData);
            await CreateAndUpdateMondayItems(mainData, mondayItems);
        }

        private static async Task CreateAndUpdateMondayItems(MainDataFetcher mainData, List<MondayItem> mondayItems)
        {
            logger.Info("Updating items on monday board");
            var mondayClient = new MondayApiClient();
            var integratedPoDict = mainData.aimsIntegrationBoard.items.ToDictionary(item => item.name);
            foreach (var newItem in mondayItems)
            {
                MondayItem oldItem;
                if (!integratedPoDict.TryGetValue(newItem.name, out oldItem)){
                    await mondayClient.CreateMondayItem(newItem);
                    logger.Info($"Creating {newItem.name}");
                }

                else if (newItem.isDifferentFromOldItem(oldItem)){
                    await mondayClient.UpdateMondayItem(oldItem, newItem);
                    logger.Info($"Updating item {oldItem.name}(item_id: {oldItem.id})");
                }
            }
        }

        private static List<MondayItem> ConvertVendorPOsToMondayItems(MainDataFetcher mainData)
        {
            logger.Info("Converting vendor POs to monday items");
            var mondayItems = mainData.vendorPOs
                                .Select(po => po.ConvertToMondayItems(mainData.aimsIntegrationBoard))
                                .SelectMany(po => po) // flattens the list of lists created in the previous line
                                .ToList();
            return mondayItems;
        }

        private static void AddAllocationDetailsToVendorPOs(MainDataFetcher mainData, Dictionary<string, List<WitreAllocationDetails>> allocationReportGroupedByVendorPO)
        {
            logger.Info("Adding allocation details to AIMS vendor PO objects");
            mainData.vendorPOs
                .Where(po => allocationReportGroupedByVendorPO.ContainsKey(po.PurchaseOrderNo))
                .ToList()
                .ForEach(po => po.allocationDetails = allocationReportGroupedByVendorPO[po.PurchaseOrderNo]);
        }

        private static Dictionary<string, List<WitreAllocationDetails>> GroupAllocationDetails(MainDataFetcher mainData)
        {
            logger.Info("Grouping allocation detail lines and summing together allocated quantities");
            var allocationReportGroupedByVendorPO = mainData.allocationDetails
                .Where(obj => obj.RecordType == RecordType.PO)
                .GroupBy(obj => obj.GetIdentifier())
                .Select(group =>
                {
                    var elementCopy = group.First().ShallowCopy();
                    elementCopy.Allocatedqty = group.Sum(item => item.Allocatedqty);
                    return elementCopy;
                })
                .GroupBy(obj => obj.WIPReference)
                .ToDictionary(group => group.Key, group => group.ToList());
            return allocationReportGroupedByVendorPO;
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