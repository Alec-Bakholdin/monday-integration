using System;
using System.Threading;
using Microsoft.Extensions.Logging;

namespace monday_integration.src.logging
{
    public static class AimsLoggerFactory
    {
        public static ILogger logger {get; set;}
        private static SemaphoreSlim _logSemaphore = new SemaphoreSlim(1, 1);

        public static AimsLogger CreateLogger(Type type) {
            return new AimsLogger(logger, type, _logSemaphore);
        }
    }
}