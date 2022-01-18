using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace monday_integration.src.functions
{
    public static class MondayHttpTrigger
    {
        [FunctionName("MondayHttpTrigger")]
        public static IActionResult Run(
            [HttpTrigger(AuthorizationLevel.Function, "get", Route = null)] HttpRequest req,
            [Queue("monday-queue")] ICollector<string> queueMessage,
            ILogger log)
        {
            queueMessage.Add("string");
            return new OkObjectResult("Successful");
        }
    }
}
