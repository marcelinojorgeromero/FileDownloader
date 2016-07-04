using System;
using System.ComponentModel;
using System.IO;
using System.Net;
using System.Threading.Tasks;

namespace FileDownloader
{
    public class WebClientEnhanced : WebClient
    {
        private const int DefaultTimeout = 100 * 1000; //100 seconds the default time for timeout
        private const int DefaultRetries = 0;
        
        private int RetryCounter { get; set; }
        
        private Tuple<string, object[]> LastMethod { get; set; }

        public event CancelEventHandler DownloadCancelled;
        public event ErrorEventHandler DownloadError;
        public event EventHandler DownloadCompletedSuccessfully;
        public event ErrorEventHandler DownloadTimeout;

        /// <summary>
        /// Time in milliseconds
        /// </summary>
        public int Timeout { get; set; }
        public int RetryMaxCount { get; set; }

        #region Constructors
        public WebClientEnhanced() : this(DefaultTimeout, DefaultRetries) { }
        public WebClientEnhanced(int timeout) : this(timeout, DefaultRetries) { }
        public WebClientEnhanced(int timeout, int retryMaxCount)
        {
            Timeout = timeout;
            RetryMaxCount = retryMaxCount;
        }
        #endregion

        private async Task<object> Invoker(string methodName, object[] parameters)
        {
            var type = GetType();
            var methodInfo = type.GetMethod(methodName);
            
            if (methodInfo.ReturnType != typeof(Task))
            {
                var result = methodInfo.Invoke(this, parameters);
                return result;
            }
            var taskResult = (Task)methodInfo.Invoke(this, parameters);
            await taskResult;
            return taskResult;
        }

        private void RemoveInvoker()
        {
            LastMethod = null;
        }
        private void StoreInvoker(string methodName, object[] parameters)
        {
            LastMethod = new Tuple<string, object[]>(methodName, parameters);
        }

        #region Method Wrappers
        public void DownloadFile(string address, string fileName)
        {
            StoreInvoker("DownloadFile", new object[] { address, fileName });
            base.DownloadFile(address, fileName);
        }
        public void DownloadFile(Uri address, string fileName)
        {
            StoreInvoker("DownloadFile", new object[] { address, fileName });
            base.DownloadFile(address, fileName);
        }
        public void DownloadFileAsync(Uri address, string fileName)
        {
            StoreInvoker("DownloadFileAsync", new object[] { address, fileName });
            base.DownloadFileAsync(address, fileName);
        }
        public void DownloadFileAsync(Uri address, string fileName, object userToken)
        {
            StoreInvoker("DownloadFileAsync", new object[] { address, fileName, userToken });
            base.DownloadFileAsync(address, fileName, userToken);
        }
        public Task DownloadFileTaskAsync(string address, string fileName)
        {
            StoreInvoker("DownloadFileTaskAsync", new object[] { address, fileName });
            return base.DownloadFileTaskAsync(address, fileName);
        }
        public Task DownloadFileTaskAsync(Uri address, string fileName)
        {
            StoreInvoker("DownloadFileTaskAsync", new object[] { address, fileName });
            return base.DownloadFileTaskAsync(address, fileName);

            //try
            //{
            //    StoreInvoker("DownloadFileTaskAsync", new object[] { address, fileName });
            //    return base.DownloadFileTaskAsync(address, fileName);
            //}
            //catch (WebException ex)
            //{
            //    if (ex.Status == WebExceptionStatus.Timeout)
            //    {
            //        DownloadTimeout?.Invoke(this, new AsyncCompletedEventArgs(ex, true, null));
            //    }
            //}
            //return null;
        }

        public string DownloadString(string address)
        {
            StoreInvoker("DownloadString", new object[] { address });
            return base.DownloadString(address);
        }
        public string DownloadString(Uri address)
        {
            StoreInvoker("DownloadString", new object[] { address });
            return base.DownloadString(address);
        }
        public void DownloadStringAsync(Uri address)
        {
            StoreInvoker("DownloadStringAsync", new object[] { address });
            base.DownloadStringAsync(address);
        }
        public void DownloadStringAsync(Uri address, object userToken)
        {
            StoreInvoker("DownloadStringAsync", new [] { address, userToken });
            base.DownloadStringAsync(address, userToken);
        }
        public Task<string> DownloadStringTaskAsync(string address)
        {
            StoreInvoker("DownloadStringTaskAsync", new object[] { address });
            return base.DownloadStringTaskAsync(address);
        }
        public Task<string> DownloadStringTaskAsync(Uri address)
        {
            StoreInvoker("DownloadStringTaskAsync", new object[] { address });
            return base.DownloadStringTaskAsync(address);
        }

        #endregion

        #region Method Overrides
        protected override WebRequest GetWebRequest(Uri address)
        {
            var request = base.GetWebRequest(address);
            if (request == null) return null;
            request.Timeout = Timeout;
            
            return request;
        }

        protected override void OnDownloadStringCompleted(DownloadStringCompletedEventArgs eventArgs)
        {
            base.OnDownloadStringCompleted(eventArgs);

            if (eventArgs.Cancelled)
            {
                RemoveInvoker();
                DownloadCancelled?.Invoke(this, new CancelEventArgs(true));
                return;
            }

            if (eventArgs.Error != null)
            {
                ProcessError(eventArgs.Error);
                return;
            }

            try
            {
                var download = eventArgs.Result;
                if (download == null) throw new NullReferenceException("Response returned null");
                RemoveInvoker();
                DownloadCompletedSuccessfully?.Invoke(this, eventArgs);
            }
            catch (Exception exception)
            {
                ProcessError(exception);
            }
            //catch (WebException ex)
            //{
            //    RemoveInvoker();

            //    if (ex.Status == WebExceptionStatus.Timeout)
            //    {
            //        DownloadTimeout?.Invoke(this, new AsyncCompletedEventArgs(ex, true, null));
            //        return;
            //    }
            //    DownloadError?.Invoke(this, new ErrorEventArgs(ex));
            //}
            //catch (Exception ex)
            //{
            //    RemoveInvoker();
            //    DownloadError?.Invoke(this, new ErrorEventArgs(ex));
            //}
        }

        protected override void OnDownloadFileCompleted(AsyncCompletedEventArgs eventArgs)
        {
            base.OnDownloadFileCompleted(eventArgs);

            if (eventArgs.Cancelled)
            {
                RemoveInvoker();
                DownloadCancelled?.Invoke(this, new CancelEventArgs(true));
                return;
            }

            if (eventArgs.Error != null)
            {
                ProcessError(eventArgs.Error);
                return;
            }

            RemoveInvoker();
            DownloadCompletedSuccessfully?.Invoke(this, eventArgs);
        }
        #endregion

        private async void ProcessError(Exception exception)
        {
            if (RetryCounter < RetryMaxCount && LastMethod != null)
            {
                RetryCounter++;
                await Invoker(LastMethod.Item1, LastMethod.Item2);
                return;
            }

            RetryCounter = 0;
            RemoveInvoker();

            if (exception.InnerException is WebException && ((WebException)exception).Status == WebExceptionStatus.Timeout)
            {
                DownloadTimeout?.Invoke(this, new ErrorEventArgs(exception.InnerException));
                return;
            }

            DownloadError?.Invoke(this, new ErrorEventArgs(exception));
        }
    }
}