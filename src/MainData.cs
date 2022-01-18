using System.Collections.Generic;
using System.Threading.Tasks;
using monday_integration.src;
using monday_integration.src.aqua;
using monday_integration.src.aqua.model;
using monday_integration.src.monday;
using monday_integration.src.monday.model;

namespace withered_tree_monday_integration.src
{
    public class MainDataFetcher
    {
        public List<WitreStyleVendorPO> vendorPOs {get; set;}
        public List<WitreAllocationDetails> allocationDetails {get; set;}
        public MondayBoard aimsIntegrationBoard {get; set;}

        private MondayIntegrationSettings settings;

        public MainDataFetcher(MondayIntegrationSettings settings) {
            this.settings = settings;
        }

        public async Task<MainDataFetcher> FetchAllInParallel() {
            var vendorPoClient = new AquaClient(settings.AimsStylePOsLineDetailsAndFieldsJobId);
            var allocationDetailsClient = new AquaClient(settings.AimsAllocationDetailsReportJobId);
            var mondayClient = new MondayApiClient();

            var tasks = new Task[]{
                fetchAqua<WitreStyleVendorPO>(vendorPoClient),
                fetchAqua<WitreAllocationDetails>(allocationDetailsClient),
                mondayClient.GetMondayBoard(settings.MondayAimsIntegrationBoardId)
            };
            Task.WaitAll(tasks);

            vendorPOs = await (Task<List<WitreStyleVendorPO>>)tasks[0];
            allocationDetails = await (Task<List<WitreAllocationDetails>>)tasks[1];
            aimsIntegrationBoard = await (Task<MondayBoard>)tasks[2];

            return this;
        }

        private async Task<List<T>> fetchAqua<T>(AquaClient aquaClient){
            await aquaClient.RerunBackgroundJob();
            return await aquaClient.FetchData<T>();
        }
    }
}