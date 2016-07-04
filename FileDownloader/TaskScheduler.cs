using System;
using System.Threading.Tasks;

namespace FileDownloader
{
    public static class TaskScheduler
    {
        public static async void Execute(Action action, int timeoutInMilliseconds)
        {
            await Task.Delay(timeoutInMilliseconds);
            action();
        }
    }
}