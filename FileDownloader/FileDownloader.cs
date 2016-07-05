using System;
using System.ComponentModel;
using System.IO;
using System.Net;
using System.Threading.Tasks;
using Infrastructure.Log;

namespace FileDownloader
{
    public class OnlineFileDownloader
    {
        private readonly string _url;
        private readonly string _fullPathWhereToSave;
        private bool _fileDownloadedSuccessfully;
        private WebClientEnhanced _client;

        public event DownloadProgressChangedEventHandler DownloadProgressChanged;
        public event EventHandler DownloadCompletedSuccessfully;
        public event CancelEventHandler DownloadCancelled;
        public event ErrorEventHandler DownloadError;
        public event ErrorEventHandler DownloadTimeout;

        public OnlineFileDownloader(string url, string fullPathWhereToSave)
        {
            if (string.IsNullOrEmpty(url)) throw new ArgumentNullException(nameof(url));
            if (string.IsNullOrEmpty(fullPathWhereToSave)) throw new ArgumentNullException(nameof(fullPathWhereToSave));

            _url = url;
            _fullPathWhereToSave = fullPathWhereToSave;
        }

        public async Task<bool> StartDownload(int timeout)
        {
            _fileDownloadedSuccessfully = false;
            try
            {
                var directoryName = Path.GetDirectoryName(_fullPathWhereToSave);
                if (directoryName == null) throw new ArgumentNullException(nameof(directoryName));

                Directory.CreateDirectory(directoryName);

                if (File.Exists(_fullPathWhereToSave))
                {
                    throw new FileFoundException();
                    //File.Delete(_fullPathWhereToSave);
                }

                _client = new WebClientEnhanced(TimeSpan.FromSeconds(timeout).Milliseconds);
                var url = new Uri(_url);
                _client.DownloadProgressChanged += DownloadProgressChanged;
                _client.DownloadCompletedSuccessfully += DownloadCompletedSuccessfully;
                _client.DownloadCancelled += DownloadCancelled;
                _client.DownloadError += DownloadError;
                _client.DownloadTimeout += DownloadTimeout;

                _client.DownloadCancelled += LocalDownloadCancelledEvent;
                _client.DownloadError += LocalDownloadErrorEvent;
                _client.DownloadTimeout += LocalDownloadTimeoutEvent;
                
                var downloadingFileTask = _client.DownloadFileTaskAsync(url, _fullPathWhereToSave);
                await downloadingFileTask;
                _client.Dispose();
                return _fileDownloadedSuccessfully && File.Exists(_fullPathWhereToSave);
            }
            catch (Exception ex)
            {
                LogManager.Instance().Error(ex);
                //DownloadError?.Invoke(ex, new ErrorEventArgs(new DownloadException(ex.Message, ex.InnerException)));
                return false;
            }
        }

        public void CancelDownload(bool deleteFile = true)
        {
            _client.CancelAsync();
            if (deleteFile) DeleteDownloadedFileIfExists();
        }

        public bool DeleteDownloadedFileIfExists()
        {
            try
            {
                if (File.Exists(_fullPathWhereToSave)) File.Delete(_fullPathWhereToSave);
                return true;
            }
            catch (Exception exception)
            {
                LogManager.Instance().Error(exception);

                return false;
            }
        }

        private void LocalDownloadTimeoutEvent(object sender, ErrorEventArgs eventArgs)
        {
            _fileDownloadedSuccessfully = false;
            LogManager.Instance().Error(eventArgs.GetException());
        }

        private void LocalDownloadCancelledEvent(object sender, CancelEventArgs eventArgs)
        {
            _fileDownloadedSuccessfully = false;
            LogManager.Instance().Info("Download Cancelled");
        }

        private void LocalDownloadErrorEvent(object sender, ErrorEventArgs eventArgs)
        {
            _fileDownloadedSuccessfully = false;
            LogManager.Instance().Error(eventArgs.GetException());
        }
    }
}