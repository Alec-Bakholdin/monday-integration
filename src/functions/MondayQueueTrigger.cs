using System;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;

namespace monday_integration.src.functions
{
    public class MondayQueueTrigger
    {
        [FunctionName("MondayQueueTrigger")]
        public void Run([QueueTrigger("monday-queue", Connection = "AzureWebJobsStorage")]string myQueueItem, ILogger log)
        {
            try{
                var task = Main.SyncMonday(log);
                task.Wait();
            }catch(Exception e) {
                log.LogError(e, "Error executing manual trigger");
            }
            return;
        }
    }
}
