
namespace Infrastructure.Log
{
    public class ErrorWebTraceInfo
    {
        public string BrowserName { get; set; }
        public string UrlReferer { get; set; }
        public string CurrentUrl { get; set; }
        public string ClientAddress { get; set; }
        public string ClientData { get; set; }
        public string UserName { get; set; }
    }
}
