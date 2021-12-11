using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using monday_integration.src.api;
using monday_integration.src.aqua;
using monday_integration.src.aqua.model;
using monday_integration.src.logging;

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
            await aquaClient.RerunBackgroundJob();
            var response = await aquaClient.FetchCSVData<WitreStylePO>();
            logger.Info(response);
        }

        private static void Initialize(ILogger logger) {
            AimsLoggerFactory.logger = logger;
            settings = new MondayIntegrationSettings(Environment.GetEnvironmentVariables());
            AimsApiFactory.InitializeApi(settings.Aims360BaseURL, settings.AimsBearerToken);
            Main.logger = AimsLoggerFactory.CreateLogger(typeof(Main));
        }

        private static void Cleanup() {
            AimsApiFactory.CloseApi();
        }
    }
}