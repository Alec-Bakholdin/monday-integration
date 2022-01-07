using System;
using System.Collections.Generic;
using System.Threading;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace monday_integration.src.logging
{
    public class AimsLogger
    {
        private ILogger logger;
        private Type type;
        private SemaphoreSlim _logSemaphore;

        public AimsLogger(ILogger logger, Type type, SemaphoreSlim logSemaphore)
        {
            this.logger = logger;
            this.type = type;
            this._logSemaphore = logSemaphore;
        }

        public void Debug(string message) {
            LogMessage(message, (msg) => logger.LogDebug(msg));
        }


        public void Info(List<object> listOfObjects) {
            var message = "";
            foreach(var obj in listOfObjects) {
                message += JsonConvert.SerializeObject(obj);
            }
            Info(message);
        }

        public void Info(object obj) {
            Info(obj.ToString());
        }

        public void Info(string message) {
            LogMessage(message, (msg) => logger.LogInformation(msg));
        }

        public void Warn(string message) {
            LogMessage(message, (msg) => logger.LogWarning(msg));
        }

        public void Error(string message) {
            LogMessage(message, (msg) => logger.LogError(msg));
        }

        private string FormatMessage(string message) {
            return $"{type.FullName}: {message}";
        }

        private void LogMessage(string message, Action<string> logFunction) {
            _logSemaphore.Wait();
            logFunction(FormatMessage(message));
            _logSemaphore.Release();
        }
    }
}