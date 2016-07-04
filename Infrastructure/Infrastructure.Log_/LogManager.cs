
namespace Infrastructure.Log
{
    public class LogManager
    {
        private static ILoggerService _loggerInstance;
        public static void Configure(string loggerName)
        {
            var config = new Log4NetConfigurator();
            config.Configure();
            _loggerInstance = new Log4NetLoggerService(loggerName);
        }

        public static ILoggerService Instance()
        {
            return _loggerInstance;
        }
    }
     
    //public class LogManager
    //{
    //    private static readonly Lazy<LogManager> _instance = new Lazy<LogManager>(() => new LogManager());
    //    internal static LogManager Instance { get { return _instance.Value; } }

    //    private ILog _logger;

    //    private LogManager() 
    //    {
    //        log4net.Config.XmlConfigurator.Configure();
    //        _logger = log4net.LogManager.GetLogger("Application.Logger");
    //    }
  
       

    //    internal void Info(string message)
    //    {
    //        _logger.Info(message);
    //    }

    //    internal void Warn(string message)
    //    {
    //        _logger.Warn(message);
    //    }

    //    internal void Debug(string message)
    //    {
    //        _logger.Debug(message);
    //    }

    //    internal void Error(string message)
    //    {
    //        _logger.Error(message);
    //    }

    //    internal void Error(Exception ex)
    //    {
    //        _logger.Error(ex.Message, ex);
    //    }

    //    internal void Fatal(string message)
    //    {
    //        _logger.Fatal(message);
    //    }

    //    internal void Fatal(Exception ex)
    //    {
    //        _logger.Fatal(ex.Message, ex);
    //    }

    //}

 
}
