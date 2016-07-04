using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Windows;
using System.Windows.Input;
using System.Windows.Controls;
using Infrastructure.Log;

namespace FileDownloader
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow //: Window
    {
        private OnlineFileDownloader _fileDownloader;

        public MainWindow()
        {
            InitializeComponent();

            //Para habilitar a todos los TextBox la selection automatica del texto
            //EventManager.RegisterClassHandler(typeof(TextBox), TextBox.GotFocusEvent, new RoutedEventHandler(TextBox_GotFocus));
            //EventManager.RegisterClassHandler(typeof(TextBox), TextBox.PreviewMouseDownEvent, new MouseButtonEventHandler(TextBox_PreviewMouseDown));

            TxtUrl.Text = @"";

            var antFolder = @"Ant Videos\";
            var downloadFolder = $@"{GetDownloadFolderPath()}\";

            var fullAntFolder = Path.Combine(downloadFolder, antFolder);
            TxtFolderPath.Text = Directory.Exists(fullAntFolder) ? fullAntFolder : Path.Combine(downloadFolder, @"Videos\");
            TxtFileTitle.Text = @".mp4";

            BtnDownload.Visibility = Visibility.Visible;
            BtnCancel.Visibility = Visibility.Hidden;
        }

        public static string GetHomePath()
        {
            // Not in .NET 2.0
            // System.Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            return Environment.OSVersion.Platform == PlatformID.Unix 
                ? Environment.GetEnvironmentVariable("HOME") 
                : Environment.ExpandEnvironmentVariables("%HOMEDRIVE%%HOMEPATH%");
        }


        public static string GetDownloadFolderPath()
        {
            if (Environment.OSVersion.Platform == PlatformID.Unix) return Path.Combine(GetHomePath(), "Downloads");

            return Convert.ToString(
                Microsoft.Win32.Registry.GetValue(
                     @"HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Explorer\Shell Folders"
                    , "{374DE290-123F-4565-9164-39C4925E467B}"
                    , string.Empty
                )
            );
        }

        public static string GetUniqueFilePath(string filepath)
        {
            if (!File.Exists(filepath)) return filepath;

            var folder = Path.GetDirectoryName(filepath) ?? string.Empty;
            var filename = Path.GetFileNameWithoutExtension(filepath);
            var extension = Path.GetExtension(filepath);
            var number = 1;

            Match regex = Regex.Match(filepath, @"(.+) \((\d+)\)\.\w+");

            if (regex.Success)
            {
                filename = regex.Groups[1].Value;
                number = int.Parse(regex.Groups[2].Value);
            }

            do
            {
                number++;
                filepath = Path.Combine(folder, $"{filename} ({number}){extension}");
            }
            while (File.Exists(filepath));

            return filepath;
        }

        private async void BtnDownload_OnClick(object sender, RoutedEventArgs e)
        {
            var url = TxtUrl.Text;
            if (!IsUrlValid(url))
            {
                UpdateStatusBar("Download URL is not valid!");
                return;
            }

            var fullPath = CombineFilePathFromTxtBoxes();

            if (File.Exists(fullPath))
            {
                var result = MessageBox.Show("Do you want the file to be renamed automatically? If no the file will be replaced.", "File already exist", MessageBoxButton.YesNoCancel);
                switch (result)
                {
                    case MessageBoxResult.Cancel:
                        UpdateStatusBar("Download Canceled");
                        return;
                    case MessageBoxResult.Yes:
                        fullPath = GetUniqueFilePath(fullPath);
                        break;
                    case MessageBoxResult.No:
                        try
                        {
                            File.Delete(fullPath);
                        }
                        catch (Exception ex)
                        {
                            LogManager.Instance().Error(ex);
                            UpdateStatusBar($"Error: {ex.Message}");
                            return;
                        }
                        break;
                }
            }

            _fileDownloader = new OnlineFileDownloader(url, fullPath);
            _fileDownloader.DownloadProgressChanged += DownloadProgressChanged;
            _fileDownloader.DownloadCompletedSuccessfully += DownloadCompletedSuccessfully;
            _fileDownloader.DownloadError += DownloadError;
            _fileDownloader.DownloadTimeout += DownloadTimeout;
            _fileDownloader.DownloadCancelled += DownloadCancelled;

            LblDownloadStatus.Text = "Downloading...";
            BtnDownload.Visibility = Visibility.Hidden;
            BtnCancel.Visibility = Visibility.Visible;

            await _fileDownloader.StartDownload(20);
        }

        private void DownloadProgressChanged(object sender, DownloadProgressChangedEventArgs e)
        {
            PbDownloadStatus.Value = e.ProgressPercentage;
        }

        private void DownloadCompletedSuccessfully(object sender, EventArgs eventArgs)
        {
            BtnDownload.Visibility = Visibility.Visible;
            BtnCancel.Visibility = Visibility.Hidden;

            LblDownloadStatus.Text = "Download finished";

            ResetProgressBar(true);
            
            //Scheduler.Execute(() =>
            //{
            //    var observableTmr = Observable.Interval(TimeSpan.FromMilliseconds(100)).Take(100);
            //    observableTmr.Subscribe(updateDownloadStatusBar, () => LblDownloadStatus.Text = "");
            //}, 2000);
        }

        private void DownloadCancelled(object sender, CancelEventArgs eventArgs)
        {
            UpdateStatusBar("Download cancelled.");
            ResetProgressBar();
        }

        private void DownloadError(object sender, ErrorEventArgs args)
        {
            BtnDownload.Visibility = Visibility.Visible;
            BtnCancel.Visibility = Visibility.Hidden;

            UpdateStatusBar($"Download error: {args.GetException().Message}");
            ResetProgressBar();
        }

        private void DownloadTimeout(object sender, ErrorEventArgs args)
        {
            BtnDownload.Visibility = Visibility.Visible;
            BtnCancel.Visibility = Visibility.Hidden;

            var isFileDeletedSuccussfully = _fileDownloader.DeleteDownloadedFileIfExists();
            var statusMsg = isFileDeletedSuccussfully 
                ? $"Download timeout: {args.GetException().Message}" 
                : "The download has timeout and the file could not be deleted. Check the log file for more.";
            UpdateStatusBar(statusMsg);
            ResetProgressBar();
        }

        private void TxtFileTitle_OnGotFocus(object sender, RoutedEventArgs e)
        {
            var text = TxtFileTitle.Text;
            var thisTxtBox = sender as TextBox;
            e.Handled = true;
            var lastIndexOfDot = text.LastIndexOf(".", StringComparison.Ordinal);
            lastIndexOfDot = lastIndexOfDot > -1 ? lastIndexOfDot : 0;
            thisTxtBox?.Select(0, text.Length - (text.Length - lastIndexOfDot));
            //TxtFileTitle.Select(0, text.Length - (text.Length - text.LastIndexOf(".", StringComparison.Ordinal)));
        }
        
        private void TxtUrl_OnGotFocus(object sender, RoutedEventArgs e)
        {
            var thisTxtBox = sender as TextBox;
            e.Handled = true;
            thisTxtBox?.SelectAll();
        }
        
        private void TxtFolderPath_OnGotFocus(object sender, RoutedEventArgs e)
        {
            var thisTxtBox = sender as TextBox;
            e.Handled = true;
            if (thisTxtBox == null) return;
            thisTxtBox.CaretIndex = thisTxtBox.Text.Length;
        }

        private void TxtBoxes_OnPreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            var thisTxtBox = sender as TextBox;
            if (thisTxtBox == null || thisTxtBox.IsKeyboardFocusWithin) return;
            thisTxtBox.Focus();
            e.Handled = true;
        }

        private void BtnOpenFolderLocation_OnClick(object sender, RoutedEventArgs e)
        {
            var dir = TxtFolderPath.Text;
            if (Directory.Exists(dir))
            {
                Process.Start("explorer.exe", dir);
                return;
            }

            var response = MessageBox.Show("Folder does not exist, do you want to create it?", "Folder missing", MessageBoxButton.YesNo);
            if (response == MessageBoxResult.No) return;

            Directory.CreateDirectory(dir);
            Process.Start("explorer.exe", dir);
        }

        private void BtnCancel_OnClick(object sender, RoutedEventArgs e)
        {
            _fileDownloader.CancelDownload();
            BtnDownload.Visibility = Visibility.Visible;
            BtnCancel.Visibility = Visibility.Hidden;
        }

        private void BtnOpenFile_OnClick(object sender, RoutedEventArgs e)
        {
            var filePath = CombineFilePathFromTxtBoxes();
            if (!File.Exists(filePath))
            {
                UpdateStatusBar("File does not exist!");
                return;
            }
            Process.Start(filePath);
        }

        private string CombineFilePathFromTxtBoxes()
        {
            var folderPath = TxtFolderPath.Text;
            var fileName = TxtFileTitle.Text;

            var fullPath = Path.Combine(folderPath, fileName);
            return fullPath;
        }

        public bool IsUrlValid(string source)
        {
            Uri uriResult;
            return Uri.TryCreate(source, UriKind.Absolute, out uriResult) && uriResult.Scheme == Uri.UriSchemeHttp;
        }

        private void UpdateStatusBar(string msg)
        {
            LblDownloadStatus.Text = msg;
            TaskScheduler.Execute(() =>
            {
                LblDownloadStatus.Text = string.Empty;
            }, 2000);
        }

        ///// <summary>
        ///// Queue of messages to show up
        ///// </summary>
        ///// <param name="msgs"></param>
        //private void UpdateStatusBar(IEnumerable<string> msgs)
        //{
        //    var msgEnumerable = msgs as IList<string> ?? msgs.ToList();
        //    msgEnumerable.Add(string.Empty);
        //    var msgLst = msgEnumerable.GetEnumerator();
        //    Action<long> updateProressBar = value =>
        //    {
        //        msgLst.MoveNext();
        //        LblDownloadStatus.Text = msgLst.Current;
        //    };
        //    updateProressBar(0);
        //    var synchContext = new SynchronizationContextScheduler(SynchronizationContext.Current);
        //    var observableTmr = Observable
        //        .Interval(TimeSpan.FromSeconds(2), TaskPoolScheduler.Default)
        //        .Take(msgEnumerable.Count - 1)
        //        .ObserveOn(synchContext);
        //    observableTmr.Subscribe(updateProressBar);
        //}

        private void ResetProgressBar(bool clearStatusText = false)
        {
            Action<long> updateProressBar = value =>
            {
                PbDownloadStatus.Value = 100 - (value + 1);
            };
            var synchContext = new SynchronizationContextScheduler(SynchronizationContext.Current);
            var observableTmr = Observable
                .Interval(TimeSpan.FromMilliseconds(10), TaskPoolScheduler.Default)
                .Take(100)
                .Delay(TimeSpan.FromMilliseconds(2000))
                .ObserveOn(synchContext);
            observableTmr.Subscribe(updateProressBar, () => { if (clearStatusText) LblDownloadStatus.Text = string.Empty; } );
        }
    }
}
