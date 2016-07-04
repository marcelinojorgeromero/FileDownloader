using System;

namespace Infrastructure.Log
{
    public interface ILoggerService
    {
        void Info(string message);
        void Warn(string message);
        void Debug(string message);
        void Error(string message);
        void Error(Exception ex);
        void ErrorWeb(Exception ex, ErrorWebTraceInfo errorInfo);
        void ErrorWeb(Exception ex, ErrorWebTraceInfo errorInfo, ErrorTypeTraceEnum errorType);
        void ErrorWeb(Exception ex, ErrorTypeTraceEnum errorType);
        void Fatal(string message);
        void Fatal(Exception ex);
    }
}
