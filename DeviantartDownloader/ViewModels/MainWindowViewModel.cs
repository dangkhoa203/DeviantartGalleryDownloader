using DeviantartDownloader.Command;
using DeviantartDownloader.Models;
using DeviantartDownloader.Models.Enum;
using DeviantartDownloader.Service;
using DeviantartDownloader.Service.Interface;
using MahApps.Metro.Controls.Dialogs;
using Microsoft.Win32;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Reflection.Metadata;
using System.Security.Policy;
using System.Text;
using System.Windows;
using System.Windows.Data;
using System.Windows.Navigation;
using static System.Net.Mime.MediaTypeNames;

namespace DeviantartDownloader.ViewModels {
    public class MainWindowViewModel : ViewModel {
        private readonly IDialogService _dialogService;
        private IDialogCoordinator _dialogCoordinator;

        private string _headerString = "";
        private int _queueLimit { get; set; } = 2;
        public ICollectionView downloadViewItems {
            get;
        }
        private readonly DeviantartService _deviantartService;
        public CancellationTokenSource cts { get; set; } = new CancellationTokenSource();


        private string _destinationPath = "";
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

        private bool _isDownloading = false;
        public bool IsDownloading {
            get {
                return _isDownloading;
            }
            set {
                _isDownloading = value;
                OnPropertyChanged(nameof(IsDownloading));
            }
        }

        private string _downloadLabel = "Download";
        public string DownloadLabel {
            get {
                return _downloadLabel;
            }
            set {
                _downloadLabel = value;
                OnPropertyChanged(nameof(DownloadLabel));
            }
        }

        private bool _isSelectAll = false;
        public bool IsSelectAll {
            get {
                return _isSelectAll;
            }
            set {
                _isSelectAll = value;
                foreach(var download in DownloadList) {
                    download.IsSelected = value;
                }
                OnPropertyChanged(nameof(IsSelectAll));

            }
        }
        public RelayCommand GetDestinationPathCommand {
            get; set;
        }
        public RelayCommand ShowSearchGalleryDialogCommand {
            get; set;
        }
        public RelayCommand ShowSettingDialogCommand {
            get; set;
        }
        public RelayCommand ShowKeySettingDialogCommand {
            get; set;
        }
        public RelayCommand RemoveDeviantFromListCommand {
            get; set;
        }
        public RelayCommand ClearListCommand {
            get; set;
        }
        public RelayCommand DownloadDeviantCommand {
            get; set;
        }
        public RelayCommand SelectAllArtCommand {
            get; set;
        }
        public RelayCommand SelectAllLiteratureCommand {
            get; set;
        }
        public RelayCommand SelectAllVideoCommand {
            get; set;
        }
        public RelayCommand SelectAllCompletedCommand {
            get; set;
        }
        public RelayCommand SelectAllFailCommand {
            get; set;
        }
        public RelayCommand ToDeviantartCommand {
            get; set;
        }


        public MainWindowViewModel(IDialogService service, DeviantartService client,IDialogCoordinator dialogCoordinator) {
            _deviantartService = client;
            _dialogCoordinator = dialogCoordinator;
            _dialogService = service;
            downloadViewItems = CollectionViewSource.GetDefaultView(_downloadList);
            RemoveDeviantFromListCommand = new RelayCommand(o => {
                RemoveDeviantFromList(o as string ?? "");
            }, o => !IsDownloading);

            ClearListCommand = new RelayCommand(o => {
                ClearList();
            }, o => !IsDownloading && DownloadList.Where(o=>o.IsSelected).ToList().Count > 0);

            GetDestinationPathCommand = new RelayCommand(o => {
                GetDownloadPath();
            }, o => !IsDownloading);

            ShowSearchGalleryDialogCommand = new RelayCommand(o => {
                ShowSearchGalleryDialog();
            }, o => !IsDownloading);

            ShowSettingDialogCommand = new RelayCommand(o => {
                ShowSettingDialog();
            }, o => !IsDownloading);
            ShowKeySettingDialogCommand = new RelayCommand(o => {
                ShowKeySettingDialog();
            }, o => !IsDownloading);
            DownloadDeviantCommand = new RelayCommand(async o => {
                await DownloadDeviant();
            }, o => { return DownloadList.Where(o => o.Status != DownloadStatus.Completed).ToList().Count > 0; });

            SelectAllArtCommand = new RelayCommand(o => {
                SelectDeviantType(DeviantType.Art);
            }, o => !IsDownloading && DownloadList.Count>0);

            SelectAllLiteratureCommand = new RelayCommand(o => {
                SelectDeviantType(DeviantType.Literature);
            }, o => !IsDownloading && DownloadList.Count > 0);

            SelectAllVideoCommand = new RelayCommand(o => {
                SelectDeviantType(DeviantType.Video);
            }, o => !IsDownloading && DownloadList.Count > 0);

            SelectAllCompletedCommand = new RelayCommand(o => {
                SelectDeviantStatus(DownloadStatus.Completed);
            }, o => !IsDownloading && DownloadList.Count > 0);

            SelectAllFailCommand = new RelayCommand(o => {
                SelectDeviantStatus(DownloadStatus.Fail);
            }, o => !IsDownloading && DownloadList.Count > 0);

            ToDeviantartCommand= new RelayCommand(o => {
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo("https://www.deviantart.com/") { UseShellExecute = true });
            }, o => true);
        }

        private void RemoveDeviantFromList(string Id) {
            var deviant = DownloadList.FirstOrDefault(d => d.Deviant.Id == Id);
            if(deviant != null) {
                DownloadList.Remove(deviant);
            }
        }
        private void ClearList() {
            foreach(var deviant in DownloadList.Where(d => d.IsSelected).ToList()) {
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
            var viewModel = _dialogService.ShowDialog<GetGalleryViewModel>(new GetGalleryViewModel(_deviantartService,_dialogCoordinator));
            
            if(viewModel.Success) {
                foreach(var deviant in viewModel.deviantViewItems.Cast<Deviant>().ToList()) {
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
            var viewModel = _dialogService.ShowDialog<SettingViewModel>(new SettingViewModel(_headerString, _queueLimit,_dialogCoordinator));
            if(viewModel.Success) {
                _headerString = viewModel.HeaderString;
                _queueLimit = int.Parse(viewModel.QueueLimit);
            }
        }
        private void ShowKeySettingDialog() {
            var viewModel = _dialogService.ShowDialog<KeySettingViewModel>(new KeySettingViewModel(_deviantartService, _dialogCoordinator));
            if(viewModel.Success) {
                
            }
        }
        private async Task DownloadDeviant() {
            if(!IsDownloading) {
                if(!Directory.Exists(DestinationPath)) {
                    await _dialogCoordinator.ShowMessageAsync(this, "ERROR", "Path not found!");
                    return;
                }
                DownloadLabel = "Cancel";
                IsDownloading = true;
                var downloadQueue = new ConcurrentQueue<DownloadableDeviant>(DownloadList);
                var throttler = new SemaphoreSlim(_queueLimit);
                var tasks = new List<Task>();
                int literatureCount = 0;
                try {
                    foreach(var deviant in downloadViewItems.Cast<DownloadableDeviant>().ToList()) {
                        if(deviant.Status != DownloadStatus.Completed) {
                            await throttler.WaitAsync(cts.Token);
                            tasks.Add(Task.Run(async () => {
                                try {
                                    if(deviant.Deviant.Type == DeviantType.Literature) {
                                        literatureCount += 1;
                                    }
                                    await _deviantartService.DownloadDeviant(deviant, cts, DestinationPath, _headerString, literatureCount);
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
                    IsDownloading = false;
                    DownloadLabel = "Download";
                    await _dialogCoordinator.ShowMessageAsync(this, "ALERT", "Download completed!");
                }
                catch {
                }
            }
            else {
                cts.Cancel();
                DownloadLabel = "Download";
                IsDownloading = false;
                cts = new CancellationTokenSource();
                await _dialogCoordinator.ShowMessageAsync(this, "ALERT", "Cancel download!");
            }
        }
        private void SelectDeviantType(DeviantType deviantType) {
            var list = DownloadList.Where(o => o.Deviant.Type == deviantType).ToList();
            if(list.Count == DownloadList.Count) {
                IsSelectAll = true;
            }
            else {
                foreach(var download in DownloadList) {
                    if(download.Deviant.Type == deviantType) {
                        download.IsSelected = true;
                    }
                }
            }
        }
        private void SelectDeviantStatus(DownloadStatus status) {
            var list = DownloadList.Where(o => o.Status == status).ToList();
            if(list.Count == DownloadList.Count) {
                IsSelectAll = true;
            }
            else {
                foreach(var download in DownloadList) {
                    if(download.Status == status) {
                        download.IsSelected = true;
                    }
                }
            }
        }
    }
}
