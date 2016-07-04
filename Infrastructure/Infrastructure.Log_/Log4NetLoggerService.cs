using System;
using log4net;

namespace Infrastructure.Log
{
    internal class Log4NetLoggerService : ILoggerService
    {
        private readonly ILog _logger;
        //private log4net.Core.Level errorWithAuditLog = new log4net.Core.Level(90000, "errorWithAuditLog");


        public Log4NetLoggerService()
        {
            //log4net.LogManager.GetRepository().LevelMap.Add(errorWithAuditLog);

            _logger = log4net.LogManager.GetLogger(
                System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
        }

        public Log4NetLoggerService(string loggerName)
        {
            //log4net.LogManager.GetRepository().LevelMap.Add(errorWithAuditLog);

            _logger = log4net.LogManager.GetLogger(loggerName);

        }

        public void Info(string message)
        {
            _logger.Info(message);
        }

        public void Warn(string message)
        {
            _logger.Warn(message);
        }

        public void Debug(string message)
        {
            _logger.Debug(message);
        }

        public void Error(string message)
        {
            _logger.Error(message);
        }

        public void Error(Exception ex)
        {
            _logger.Error(ex.Message, ex);
        }

        public void ErrorWeb(Exception ex, ErrorWebTraceInfo errorInfo)
        {
            ErrorWeb(ex, errorInfo, ErrorTypeTraceEnum.Error);
        }

        public void ErrorWeb(Exception ex, ErrorWebTraceInfo errorInfo, ErrorTypeTraceEnum errorType)
        {
            ThreadContext.Properties["BrowserName"] = errorInfo.BrowserName;
            ThreadContext.Properties["ClientAddress"] = errorInfo.ClientAddress;
            ThreadContext.Properties["CurrentUrl"] = errorInfo.CurrentUrl;
            ThreadContext.Properties["UrlReferer"] = errorInfo.UrlReferer;
            ThreadContext.Properties["ClientData"] = errorInfo.ClientData;
            ThreadContext.Properties["UserName"] = errorInfo.UserName;

            switch (errorType)
            {
                case ErrorTypeTraceEnum.Error:
                    _logger.Error(ex.Message, ex);
                    break;
                case ErrorTypeTraceEnum.ErrorWithAudit:
                    Log4NetExtensionLevel.ErrorWithAudit(_logger, ex.Message, ex);
                    break;
            }

        }

        public void Fatal(string message)
        {
            _logger.Fatal(message);
        }

        public void Fatal(Exception ex)
        {
            _logger.Fatal(ex.Message, ex);
        }


        public void ErrorWeb(Exception ex, ErrorTypeTraceEnum errorType)
        {
            switch (errorType)
            {
                case ErrorTypeTraceEnum.Error:
                    _logger.Error(ex.Message, ex);
                    break;
                case ErrorTypeTraceEnum.ErrorWithAudit:
                    Log4NetExtensionLevel.ErrorWithAudit(_logger, ex.Message, ex);
                    break;
            }
        }
    }
}
