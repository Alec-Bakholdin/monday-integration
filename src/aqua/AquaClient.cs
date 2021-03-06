using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ComposableAsync;
using monday_integration.src.api;
using monday_integration.src.aqua.model;
using monday_integration.src.logging;
using RateLimiter;

namespace monday_integration.src.aqua {
    public class AquaClient {
        private const uint MAX_FETCH_ITERATIONS = 50;
        private static AimsLogger logger = AimsLoggerFactory.CreateLogger(typeof(AquaClient));

        private AimsApi api;
        private TimeLimiter rateLimiter;
        private string jobId;
        private string publishLink;

        public AquaClient(string jobId) {
            this.jobId = jobId;
            this.api = AimsApiFactory.GetApi();
            this.rateLimiter = TimeLimiter.GetFromMaxCountByInterval(1, TimeSpan.FromSeconds(2));
        }

        public async Task<AquaRerunResponse> RerunBackgroundJob() {
            logger.Info($"Rerunning job {jobId}");
            var resource = $"/jobsmanagement/v1.0/backgroundjob/{jobId}/rerun";
            var response = await api.PostAsync<AquaRerunResponse>(resource, null);
            this.publishLink = response.publishLink;
            logger.Info($"Successfully reran job {jobId}");
            return response;
        }

        public async Task<List<T>> FetchData<T>(AquaDataType dataType = AquaDataType.CSV)
        {
            await WaitForJobCompletion();
            switch(dataType) {
                case AquaDataType.JSON:
                    return (await api.GetAsync<AquaPublishLinkResponse<T>>(publishLink)).data;
                case AquaDataType.CSV:
                    return await api.GetAsync<List<T>>(publishLink);
                default:
                    throw new NotSupportedException($"Data type {dataType} is not supported yet");
            }
        }

        private async Task WaitForJobCompletion()
        {
            var jobByBackgroundIdResource = $"/jobsmanagement/v1.0/backgroundjob/{jobId}";
            AquaJobByBackgroundIdResponse response = null;
            for (int i = 0; i < MAX_FETCH_ITERATIONS; i++)
            {
                await rateLimiter;
                response = await api.GetAsync<AquaJobByBackgroundIdResponse>(jobByBackgroundIdResource);

                if (response.jobStatus == AquaJobStatus.Completed) {
                    logger.Info($"{response.jobStatusText} ({jobId})");
                    publishLink = response.publishLink;
                    return;
                } else if (i % 4 == 0) {
                    logger.Info($"JobId - {jobId}, status - {response.jobStatus} {response.jobStatusText}");
                }
            }
            throw new AquaException($"Job {jobId} didn't complete on time ({response?.jobStatusText})");
        }
    }
}