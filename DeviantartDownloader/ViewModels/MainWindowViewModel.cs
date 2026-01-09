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
using System.IO;
using System.Net;
using System.Net.Http;
using System.Reflection.Metadata;
using System.Security.Policy;
using System.Text;
using System.Windows;
using System.Windows.Navigation;
using static System.Net.Mime.MediaTypeNames;

namespace DeviantartDownloader.ViewModels {
    public class MainWindowViewModel : ViewModel {
        private readonly IDialogService _dialogService;
        public DeviantartService DeviantartService {
            get; set;
        }
        public CancellationTokenSource cts { get; set; } = new CancellationTokenSource();


        private string _destinationPath="";
        public string DestinationPath {
            get {
                return _destinationPath;
            }
            set {
                _destinationPath = value;
                OnPropertyChanged(nameof(DestinationPath));
            }
        }

        private ObservableCollection<DownloadableDeviant> _downloadList = [];
        public ObservableCollection<DownloadableDeviant> DownloadList {
            get {
                return _downloadList;
            }
            set {
                _downloadList = value;
                OnPropertyChanged(nameof(DownloadList));
            }
        }

        private bool _isDownloading=false;
        public bool IsDownloading {
            get {
                return _isDownloading;
            }
            set {
                _isDownloading = value;
                OnPropertyChanged(nameof(IsDownloading));
            }
        }
        private string _downloadLabel= "Download";
        public string DownloadLabel {
            get {
                return _downloadLabel;
            }
            set {
                _downloadLabel = value;
                OnPropertyChanged(nameof(DownloadLabel));
            }
        }
        private int _queueLimit { get; set; } = 2;
        public RelayCommand GetDestinationPathCommand {
            get; set;
        }
        public RelayCommand ShowSearchGalleryDialogCommand {
            get; set;
        }
        public RelayCommand ShowSettingDialogCommand {
            get; set;
        }
        public RelayCommand RemoveDeviantFromListCommand {
            get; set;
        }
        public RelayCommand ClearListCommand {
            get; set;
        }
        public RelayCommand ClearCompletedFromListCommand {
            get; set;
        }
        public RelayCommand DonwloadDeviantCommand {
            get; set;
        }
        private string _headerString = "";


        public MainWindowViewModel(IDialogService service, DeviantartService client) {
            DeviantartService = client;
            _dialogService = service;
            RemoveDeviantFromListCommand = new RelayCommand(o => {
                RemoveDeviantFromList(o as string ?? "");
            }, o => !IsDownloading);

            ClearListCommand = new RelayCommand(o => {
                ClearList();
            }, o => !IsDownloading && DownloadList.Count > 0);

            ClearCompletedFromListCommand = new RelayCommand(o => {
                ClearCompletedFromList();
            }, o => !IsDownloading && DownloadList.Where(o => o.Status == DownloadStatus.Completed).ToList().Count > 0);

            GetDestinationPathCommand = new RelayCommand(o => {
                GetDownloadPath();
            }, o => !IsDownloading);

            ShowSearchGalleryDialogCommand = new RelayCommand(o => {
                ShowSearchGalleryDialog();
            }, o => !IsDownloading);

            ShowSettingDialogCommand = new RelayCommand(o => {
                ShowSettingDialog();
            }, o => !IsDownloading);

            DonwloadDeviantCommand = new RelayCommand(async o => {
                await DownloadDeviant();
            }, o => { return DownloadList.Where(o => o.Status != DownloadStatus.Completed).ToList().Count > 0; });
        }

        private void RemoveDeviantFromList(string Id) {
            var deviant = DownloadList.FirstOrDefault(d => d.Deviant.Id == Id);
            if(deviant != null) {
                DownloadList.Remove(deviant);
            }
        }
        private void ClearList() {
            DownloadList.Clear();
        }
        private void ClearCompletedFromList() {
            foreach(var deviant in DownloadList.Where(o => o.Status == DownloadStatus.Completed).ToList()) {
                DownloadList.Remove(deviant);
            }
        }
        private void GetDownloadPath() {
            var folderDialog = new OpenFolderDialog {
                Title = "Select Folder",
                InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.Desktop)
            };

            bool? result = folderDialog.ShowDialog();
            if(result == true) {
                string folderName = folderDialog.FolderName;
                DestinationPath = folderName;
            }
        }
        private void ShowSearchGalleryDialog() {
            var viewModel = _dialogService.ShowDialog<GetGalleryViewModel>(new GetGalleryViewModel(DeviantartService));

            if(viewModel.Success) {
                foreach(var deviant in viewModel.Deviants) {
                    var downloadableDeviant = DownloadList.FirstOrDefault(o => o.Deviant.Id == deviant.Id);
                    if(downloadableDeviant == null) {
                        DownloadList.Add(new(deviant));
                    }
                    else if(downloadableDeviant.Status == DownloadStatus.Completed) {
                        downloadableDeviant.Percent = 0;
                        downloadableDeviant.Status = DownloadStatus.Waiting;
                        downloadableDeviant.DownloadSpeed = "";
                    }

                }
            }
        }
        private void ShowSettingDialog() {
            var viewModel = _dialogService.ShowDialog<SettingViewModel>(new SettingViewModel(_headerString, _queueLimit));
            if(viewModel.Success) {
                _headerString = viewModel.HeaderString;
                _queueLimit = int.Parse(viewModel.QueueLimit);
            }


        }
        private async Task DownloadDeviant() {
            if(!IsDownloading) {

                if(!Directory.Exists(DestinationPath)) {
                    MessageBox.Show("Path not found!", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }
                DownloadLabel = "Cancel";
                IsDownloading = true;
                var downloadQueue = new ConcurrentQueue<DownloadableDeviant>(DownloadList);
                var throttler = new SemaphoreSlim(_queueLimit);
                var tasks = new List<Task>();
                using var client = new HttpClient();
                int literatureCount = 0;
                try {
                    foreach(var deviant in downloadQueue) {

                        if(deviant.Status != DownloadStatus.Completed) {
                            await throttler.WaitAsync(cts.Token);
                            tasks.Add(Task.Run(async () => {
                                try {
                                    if(deviant.Deviant.Type == DeviantType.Literature) {
                                        literatureCount += 1;
                                    }
                                    await DeviantartService.DonwloadDeviant(deviant, cts, DestinationPath, _headerString, literatureCount);
                                }
                                catch(Exception ex) {

                                }
                                finally {
                                  
                                   
                                    throttler.Release();
                                }
                            }, cts.Token));
                        }
                    }

                    await Task.WhenAll(tasks);

                    var test = DownloadList.ToList();
                    DownloadList.Clear();
                    foreach(var d in test) {
                        DownloadList.Add(d);
                    }
                    IsDownloading = false;
                    DownloadLabel = "Download";
                }
                catch {

                }
                finally {
                    MessageBox.Show("Download completed!", "Download", MessageBoxButton.OK, MessageBoxImage.Exclamation);
                }

            }
            else {
                cts.Cancel();
                DownloadLabel = "Download";
                IsDownloading = false;
                var test = DownloadList.ToList();
                DownloadList.Clear();
                foreach(var d in test) {
                    DownloadList.Add(d);
                }
                cts = new CancellationTokenSource();
                MessageBox.Show("Opperation canceled", "Information", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }
    }
}
