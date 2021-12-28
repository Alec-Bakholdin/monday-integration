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
            var aquaClient = new AquaClient(settings.AimsJobId);
            //await aquaClient.RerunBackgroundJob();
            var stylePOs = await aquaClient.FetchJsonData<WitreStylePO>();

            var mondayClient = new MondayClient();
            var options = new MondayBoardBodyOptions(){name=true, id=true, workspace_id=true};
            var boards = await mondayClient.GetMondayBoards(options);
            var vendorBoardIdDict = new Dictionary<string, string>();
            foreach(var board in boards) {
                if(board.workspace_id == null) continue;
                var match = new Regex(@"^([\d\w]+) - [\w\s]+$").Match(board.name);
                if(match.Success) {
                    var vendor = match.Groups[1].Value;
                    vendorBoardIdDict[vendor] = board.id;
                    logger.Info($"{vendor}: {board.id}");
                }
            }
            var mondayItems = mondayClient.MapWitreStylePosToMondayItems(stylePOs, vendorBoardIdDict);
            mondayItems = mondayItems.Where(item => item.board_id != null).ToList();
            foreach(var mondayItem in mondayItems) {
                await mondayClient.CreateMondayItem(mondayItem);
                foreach(var subitem in mondayItem.subitems) {
                    await mondayClient.CreateMondaySubitem(subitem);
                }
            }

            //foreach(var board in listOfBoards) {
            //    logger.Info(board);
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