using System;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Extensions.Logging;

namespace monday_integration.src.functions
{
    public class MondayTimerTrigger
    {
        [FunctionName("MondayTimerTrigger")]
        public void Run([TimerTrigger("0 0 0,12 * * *")]TimerInfo myTimer, ILogger log)
        {
            var task = Main.SyncMonday(log);
            task.Wait();
        }
    }
}
