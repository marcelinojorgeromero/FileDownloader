using System.Windows;
using Infrastructure.Log;

namespace FileDownloader
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        public App()
        {
            LogManager.Configure("FileDownloaderLog");
        }
    }
}
