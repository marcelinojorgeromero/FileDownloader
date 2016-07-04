namespace Infrastructure.Log
{
    internal class Log4NetConfigurator
    {
        public void Configure()
        {
            log4net.Config.XmlConfigurator.Configure();
        }
    }
}
