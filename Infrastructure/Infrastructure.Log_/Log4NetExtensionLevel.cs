using System;
using log4net;

namespace Infrastructure.Log
{
    public static class Log4NetExtensionLevel
    {
        private static readonly log4net.Core.Level ErrorWithAuditLog = new log4net.Core.Level(90000, "errorWithAuditLog");


        public static void ErrorWithAudit(ILog log, string message, Exception ex)
        {
            log4net.LogManager.GetRepository().LevelMap.Add(ErrorWithAuditLog);
            log.Logger.Log(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType,
                ErrorWithAuditLog, message, ex);
        }

        public static void ErrorWithAudit(ILog log, string message, Exception ex, params object[] args)
        {
            log4net.LogManager.GetRepository().LevelMap.Add(ErrorWithAuditLog);
            var formattedMessage = string.Format(message, args);
            log.Logger.Log(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType,
                ErrorWithAuditLog, formattedMessage, ex);
        }
    }
}
