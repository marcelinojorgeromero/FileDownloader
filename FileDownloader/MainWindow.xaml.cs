﻿using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
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
            var downloadFolder = $@"{GetDownloadFolderPath()}\"; //@"\\Mac\Home\Downloads\Ant Videos\";

            var fullAntFolder = Path.Combine(downloadFolder, antFolder);
            TxtFolderPath.Text = Directory.Exists(fullAntFolder) ? fullAntFolder : Path.Combine(downloadFolder, @"Pluralsight\");
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
            var folderPath = TxtFolderPath.Text;
            var fileName = TxtFileTitle.Text;

            var fullPath = Path.Combine(folderPath, fileName);

            if (File.Exists(fullPath))
            {
                var result = MessageBox.Show("Do you want the file to be renamed automatically? If no the file will be replaced.", "File already exist", MessageBoxButton.YesNoCancel);
                switch (result)
                {
                    case MessageBoxResult.Cancel:
                        LblDownloadStatus.Text = "Download Canceled";
                        TaskScheduler.Execute(() =>
                        {
                            LblDownloadStatus.Text = string.Empty;
                        }, 2000);
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
                            LblDownloadStatus.Text = $"Error: {ex.Message}";
                            TaskScheduler.Execute(() =>
                            {
                                LblDownloadStatus.Text = string.Empty;
                            }, 2000);

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
            LblDownloadStatus.Text = "Download finished";

            Action<long> updateDownloadStatusBar = value =>
            {
                PbDownloadStatus.Value = 100 - (value + 1);
            };
            var synchContext = new SynchronizationContextScheduler(SynchronizationContext.Current);
            var observableTmr = Observable
                .Interval(TimeSpan.FromMilliseconds(10), TaskPoolScheduler.Default)
                .Take(100)
                .Delay(TimeSpan.FromMilliseconds(2000))
                .ObserveOn(synchContext);
            observableTmr.Subscribe(updateDownloadStatusBar, () => LblDownloadStatus.Text = "");

            //Scheduler.Execute(() =>
            //{
            //    var observableTmr = Observable.Interval(TimeSpan.FromMilliseconds(100)).Take(100);
            //    observableTmr.Subscribe(updateDownloadStatusBar, () => LblDownloadStatus.Text = "");
            //}, 2000);
        }

        private void DownloadCancelled(object sender, CancelEventArgs eventArgs)
        {
            LblDownloadStatus.Text = "Download cancelled.";
            TaskScheduler.Execute(() =>
            {
                LblDownloadStatus.Text = string.Empty;
            }, 2000);
        }

        private void DownloadError(object sender, ErrorEventArgs args)
        {
            LblDownloadStatus.Text = $"Download error: {args.GetException().Message}";
        }

        private void DownloadTimeout(object sender, ErrorEventArgs args)
        {
            LblDownloadStatus.Text = $"Download timeout: {args.GetException().Message}";
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
            BtnDownload.Visibility = Visibility.Visible;
            BtnCancel.Visibility = Visibility.Hidden;
            _fileDownloader.CancelDownload();
        }
    }
}