using DeviantartDownloader.Command;
using DeviantartDownloader.Models;
using DeviantartDownloader.Models.Enum;
using DeviantartDownloader.Service;
using DeviantartDownloader.Service.Interface;
using Microsoft.Win32;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Net;
using System.Net.Http;
using System.Reflection.Metadata;
using System.Security.Policy;
using System.Text;
using System.Windows;
using System.Windows.Navigation;
using static System.Net.Mime.MediaTypeNames;

namespace DeviantartDownloader.ViewModels
{
    public class MainWindowViewModel:ViewModel
    {
        private readonly IDialogService _dialogService;
        public DeviantartService DeviantartService { get; set; }
        public CancellationTokenSource cts { get; set; } = new CancellationTokenSource();


        private string _destinationPath;
        public string DestinationPath {
            get { return _destinationPath; }
            set { _destinationPath = value; OnPropertyChanged(nameof(DestinationPath)); }
        }

        private ObservableCollection<DownloadableDeviant> _downloadList;
        public ObservableCollection<DownloadableDeviant> DownloadList {  
            get { return _downloadList; }
            set { _downloadList = value;
                  OnPropertyChanged(nameof(DownloadList)); } 
        }

        private bool _isDownloading;
        public bool IsDownloading { 
            get { return _isDownloading; }
            set { _isDownloading = value;
                  OnPropertyChanged(nameof(IsDownloading)); } 
        }
        private string _downloadLabel;
        public string DownloadLabel { 
            get { return _downloadLabel; }
            set { _downloadLabel = value;
                  OnPropertyChanged(nameof(DownloadLabel));}
        }
        public RelayCommand GetDestinationPathCommand { get; set; }
        public RelayCommand ShowSearchGalleryDialogCommand { get; set; }
        public RelayCommand ShowCookieSettingDialogCommand { get; set; }
        public RelayCommand RemoveDeviantFromListCommand { get; set; }
        public RelayCommand ClearListCommand { get; set; }
        public RelayCommand DonwloadDeviantCommand { get; set; }
        private string _headerString="";
        public string HeaderString {
            get { return _headerString; }
            set { _headerString = value;OnPropertyChanged(nameof(HeaderString)); }
        }
       
        public MainWindowViewModel(IDialogService service, DeviantartService client) {
            DeviantartService = client;
            IsDownloading = false;
            _destinationPath = string.Empty;
            _dialogService = service;
            _downloadList = [];
            _downloadLabel = "Download";
            RemoveDeviantFromListCommand = new RelayCommand(o => {
                RemoveDeviantFromList(o as string ?? "");
            }, o => !IsDownloading);

            ClearListCommand = new RelayCommand(o => {
                ClearList(); 
            }, o => !IsDownloading && DownloadList.Count > 0);

            GetDestinationPathCommand = new RelayCommand(o => {
                GetDownloadPath();
            }, o => !IsDownloading);

            ShowSearchGalleryDialogCommand = new RelayCommand(o => {
                ShowSearchGalleryDialog();
            }, o => !IsDownloading);

            ShowCookieSettingDialogCommand = new RelayCommand(o => {
                ShowCookieSettingDialog();
            }, o => !IsDownloading);
            DonwloadDeviantCommand = new RelayCommand(async o => {
                await DownloadDeviant();
            }, o => { return DownloadList.Where(o=>o.Status!=DownloadStatus.Completed).ToList().Count > 0 &&
                             DestinationPath.Count() > 0; });
        }

        private void RemoveDeviantFromList(string Id) {
            var deviant = DownloadList.FirstOrDefault(d => d.Deviant.Id == Id);
            if (deviant != null) {
                DownloadList.Remove(deviant);
            }
        }
        private void ClearList() {
            DownloadList.Clear();
        }
        private void GetDownloadPath() {
            var folderDialog = new OpenFolderDialog {
                Title = "Select Folder",
                InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.Desktop)
            };

            bool? result = folderDialog.ShowDialog();
            if (result == true) {
                string folderName = folderDialog.FolderName;
                DestinationPath = folderName;
            }
        }
        private void ShowSearchGalleryDialog() {
            var viewModel = _dialogService.ShowDialog<GetGalleryViewModel>(new GetGalleryViewModel(DeviantartService));

            if (viewModel.Success) {
                foreach (var deviant in viewModel.Deviants) {
                    if (DownloadList.FirstOrDefault(o => o.Deviant.Id == deviant.Id) == null) {
                        DownloadList.Add(new(deviant));
                    }

                }
            }
        }
        private void ShowCookieSettingDialog() {
            var viewModel = _dialogService.ShowDialog<CookieSettingViewModel>(new CookieSettingViewModel(HeaderString));
            if (viewModel.Success) {
                HeaderString= viewModel.HeaderString;
            }


        }
        private async Task DownloadDeviant() {
            if (!IsDownloading) {
                DownloadLabel = "Cancel";
                IsDownloading = true;
                var downloadQueue = new ConcurrentQueue<DownloadableDeviant>(DownloadList);
                var throttler = new SemaphoreSlim(1);
                var tasks = new List<Task>();
                using var client = new HttpClient();

                foreach (var deviant in downloadQueue) {
                    // Wait for a slot to become available in the semaphore
                    await throttler.WaitAsync();

                    // Start the download task
                    tasks.Add(Task.Run(async () =>
                    {
                        try {
                            await DeviantartService.DonwloadDeviant(deviant, cts, DestinationPath,HeaderString);
                        }
                        catch (Exception ex) {

                        }
                        finally {
                            // Release the slot so another download can start
                            throttler.Release();
                        }
                    }, cts.Token));
                }

                // Wait for all initiated tasks to complete
                await Task.WhenAll(tasks);

                var test = DownloadList.ToList();
                DownloadList.Clear();
                foreach (var d in test) {
                    DownloadList.Add(d);
                }
                IsDownloading = false;
                DownloadLabel = "Download";
            }
            else {
                cts.Cancel();
                MessageBox.Show("Opperation canceled", "Information", MessageBoxButton.OK, MessageBoxImage.Information);
                DownloadLabel = "Download";
                IsDownloading = false;
                var test = DownloadList.ToList();
                DownloadList.Clear();

                foreach (var d in test) {
                    DownloadList.Add(d);
                }
                cts = new CancellationTokenSource();
            }
        }
    }
}
