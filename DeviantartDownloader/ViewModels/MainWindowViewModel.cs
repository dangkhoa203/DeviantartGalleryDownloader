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
using System.Text.Json;
using static System.Net.Mime.MediaTypeNames;

namespace DeviantartDownloader.ViewModels {
    public class MainWindowViewModel : ViewModel {
        private readonly IDialogService _dialogService;
        private IDialogCoordinator _dialogCoordinator;
        
        public ICollectionView downloadViewItems {
            get;
        }
        private readonly DeviantartService _deviantartService;
        public CancellationTokenSource cts { get; set; } = new CancellationTokenSource();
        public AppSetting AppSetting { get; set; }
        private bool _isImporting=false;

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
        private DownloadStatus? _downloadStatusMode = null;
        private DeviantType? _downloadTypeMode = null;
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
        public RelayCommand ShowDownloadSettingDialogCommand {
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
        public RelayCommand SelectAllTierLockedCommand {
            get; set;
        }
        public RelayCommand ToDeviantartCommand {
            get; set;
        }
        public RelayCommand ToProjectPageCommand {
            get; set;
        }
        public RelayCommand ExportListCommand {
            get; set;
        }
        public RelayCommand ImportListCommand {
            get; set;
        }

        public MainWindowViewModel(IDialogService service, DeviantartService client,IDialogCoordinator dialogCoordinator) {
            AppSetting = new();
            _deviantartService = client;
            _dialogCoordinator = dialogCoordinator;
            _dialogService = service;
            downloadViewItems = CollectionViewSource.GetDefaultView(_downloadList);
            RemoveDeviantFromListCommand = new RelayCommand(o => {
                RemoveDeviantFromList(o as string ?? "");
            }, o => !IsDownloading && !_isImporting);

            ClearListCommand = new RelayCommand(o => {
                ClearList();
            }, o => !IsDownloading && !_isImporting && DownloadList.Where(o=>o.IsSelected).ToList().Count > 0);

            GetDestinationPathCommand = new RelayCommand(o => {
                GetDownloadPath();
            }, o => !IsDownloading && !_isImporting);

            ShowSearchGalleryDialogCommand = new RelayCommand(o => {
                ShowSearchGalleryDialog();
            }, o => !IsDownloading && !_isImporting);

            ShowSettingDialogCommand = new RelayCommand(o => {
                ShowSettingDialog();
            }, o => !IsDownloading && !_isImporting);

            ShowKeySettingDialogCommand = new RelayCommand(o => {
                ShowKeySettingDialog();
            }, o => !IsDownloading && !_isImporting);

            ShowDownloadSettingDialogCommand = new RelayCommand(o => {
                ShowDownloadModeDialog();
            }, o => !IsDownloading && !_isImporting);

            DownloadDeviantCommand = new RelayCommand(async o => {
                await DownloadDeviant();
            }, o => { return DownloadList.Where(o => o.Status != DownloadStatus.Completed)
                                         .Where(o => _downloadStatusMode != null ? o.Status==_downloadStatusMode : true)
                                         .Where(o=> _downloadTypeMode!=null ? o.Deviant.Type==_downloadTypeMode : true)
                                         .ToList().Count > 0; });

            SelectAllArtCommand = new RelayCommand(o => {
                SelectDeviantType(DeviantType.Art);
            }, o => !IsDownloading && !_isImporting && DownloadList.Count>0);

            SelectAllLiteratureCommand = new RelayCommand(o => {
                SelectDeviantType(DeviantType.Literature);
            }, o => !IsDownloading && !_isImporting && DownloadList.Count > 0);

            SelectAllVideoCommand = new RelayCommand(o => {
                SelectDeviantType(DeviantType.Video);
            }, o => !IsDownloading && !_isImporting && DownloadList.Count > 0);

            SelectAllCompletedCommand = new RelayCommand(o => {
                SelectDeviantStatus(DownloadStatus.Completed);
            }, o => !IsDownloading && !_isImporting && DownloadList.Count > 0);

            SelectAllFailCommand = new RelayCommand(o => {
                SelectDeviantStatus(DownloadStatus.Fail);
            }, o => !IsDownloading && !_isImporting && DownloadList.Count > 0);

            SelectAllTierLockedCommand = new RelayCommand(o => {
                SelectDeviantStatus(DownloadStatus.Tier_Locked);
            }, o => !IsDownloading && !_isImporting && DownloadList.Count > 0);

            ToDeviantartCommand = new RelayCommand(o => {
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo("https://www.deviantart.com/") { UseShellExecute = true });
            }, o => true);

            ToProjectPageCommand = new RelayCommand(o => {
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo("https://github.com/dangkhoa203/Deviantart-Gallery-Downloader") { UseShellExecute = true });
            }, o => true);

            ImportListCommand = new RelayCommand(async o => {
                await getJSONfile();
            }, o => !IsDownloading && !_isImporting);

            ExportListCommand = new RelayCommand(async o => {
                await exportJSONFile();
            }, o => !IsDownloading && !_isImporting && DownloadList.Count > 0);
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
        private async Task getJSONfile() {
            var JSONDialog = new OpenFileDialog {
                Title = "Select JSON file",
                InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.Desktop),
                Filter = "JSON files (*.json)|*.json"
            };

            bool? result = JSONDialog.ShowDialog();
            if(result == true) {
                IsDownloading = true;
                _isImporting = true;
                string jsonString = File.ReadAllText(JSONDialog.FileName);
                List<DownloadableDeviant> myData = JsonSerializer.Deserialize<List<DownloadableDeviant>>(jsonString);
                DownloadList.Clear();
                foreach(var deviant in myData) {
                    if(deviant.Deviant == null ||
                       deviant.Deviant.Id == "" ||
                       deviant.Deviant.Author == null ||
                       deviant.Deviant.Url == "") {
                        continue;
                    }
                    DownloadList.Add(deviant);
                }
                IsDownloading = false;
                _isImporting = false;
            }
        }
        private async Task exportJSONFile() {
            if(!Directory.Exists(DestinationPath)) {
                await _dialogCoordinator.ShowMessageAsync(this, "ERROR", "Path not found!");
                return;
            }
            var options = new JsonSerializerOptions { WriteIndented = true };
            var filePath = Path.Combine(DestinationPath, "DeviantartDownloadList.json");
            string jsonString = JsonSerializer.Serialize(DownloadList, options);
            await File.WriteAllTextAsync(filePath, jsonString);
            await _dialogCoordinator.ShowMessageAsync(this, "ALERT", $"File has been exported to {DestinationPath} .");
        }

        private void ShowSearchGalleryDialog() {
            var viewModel = _dialogService.ShowDialog<GetGalleryViewModel>(new GetGalleryViewModel(_deviantartService,_dialogCoordinator,AppSetting));
            
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
            var viewModel = _dialogService.ShowDialog<SettingViewModel>(new SettingViewModel(AppSetting,_dialogCoordinator));
            if(viewModel.Success) {
                AppSetting.HeaderString = viewModel.HeaderString;
                AppSetting.QueueLimit = int.Parse(viewModel.QueueLimit);
                AppSetting.UserKeySearchDeviantWaitTime = int.Parse(viewModel.DeviantSearchWaitTime);
                AppSetting.UserKeyDownloadDeviantWaitTime = int.Parse(viewModel.DeviantDownloadWaitTime);
                AppSetting.UserKeySearchFolderWaitTime = int.Parse(viewModel.FolderSearchWaitTime);
                AppSetting.DownloadDescription = viewModel.DownloadDescription;
                AppSetting.DownloadDescriptionOnly = viewModel.DescriptionOnly;
                AppSetting.IgnoreEmptyDescription = viewModel.IgnoreEmptyDescription;
                AppSetting.CustomStyle = viewModel.CustomStyle;
                AppSetting.UseCustomStyle= viewModel.UseCustomStyle;
            }
        }
        private void ShowKeySettingDialog() {
            var viewModel = _dialogService.ShowDialog<KeySettingViewModel>(new KeySettingViewModel(_deviantartService, _dialogCoordinator));
            if(viewModel.Success) {
            }   
        }
        private void ShowDownloadModeDialog() {
            var viewModel = _dialogService.ShowDialog<DownloadModeViewModel>(new DownloadModeViewModel(_dialogCoordinator,_downloadTypeMode,_downloadStatusMode));
            if(viewModel.Success) {
                _downloadTypeMode = viewModel.SelectedType;
                _downloadStatusMode = viewModel.SelectedStatus;
            }
        }
        private async Task DownloadDeviant() {
            if(!IsDownloading) {
                cts = new CancellationTokenSource();
                if(!Directory.Exists(DestinationPath)) {
                    await _dialogCoordinator.ShowMessageAsync(this, "ERROR", "Path not found!");
                    return;
                }
                var downloadList = downloadViewItems.Cast<DownloadableDeviant>().Where(o=>o.Status!=DownloadStatus.Tier_Locked);
                if(_downloadTypeMode != null) {
                    downloadList = downloadList.Where(o => o.Deviant.Type == _downloadTypeMode);
                }
                if(_downloadStatusMode != null) {
                    downloadList = downloadList.Where(o => o.Status == _downloadStatusMode);
                }
                if(AppSetting.DownloadDescription) {
                    var cancelDescription = new CancellationTokenSource();

                    var settings = new MetroDialogSettings() {
                        NegativeButtonText = "Cancel",
                    };
                    var controller=await _dialogCoordinator.ShowProgressAsync(this, "Downloading description", "",true,settings);
                    controller.Minimum = 0;
                    controller.Maximum = 100;
                    controller.SetProgress(0);
                    
                    await _deviantartService.GetDescriptions(downloadList.ToList(),
                                                            cancelDescription,
                                                            DestinationPath,
                                                            AppSetting,
                                                            controller);
                }
                if(!AppSetting.DownloadDescriptionOnly) {
                    DownloadLabel = "Cancel";
                    IsDownloading = true;
                    var downloadQueue = new ConcurrentQueue<DownloadableDeviant>(DownloadList);
                    var throttler = new SemaphoreSlim(AppSetting.QueueLimit);
                    var tasks = new List<Task>();
                    int literatureCount = 0;
                    try {
                        downloadList = downloadList.ToList();
                        foreach(var deviant in downloadList) {
                            if(deviant.Status != DownloadStatus.Completed) {
                                await throttler.WaitAsync(cts.Token);
                                tasks.Add(Task.Run(async () => {
                                    try {
                                        if(deviant.Deviant.Type == DeviantType.Literature) {
                                            literatureCount += 1;
                                        }
                                        await _deviantartService.DownloadDeviant(deviant, cts, DestinationPath, AppSetting, literatureCount);
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
                        if(!cts.IsCancellationRequested) {
                            await _dialogCoordinator.ShowMessageAsync(this, "ALERT", "Download completed!");
                        }
                    }
                    catch {
                    }
                }
                else {
                    await _dialogCoordinator.ShowMessageAsync(this, "ALERT", "Download description completed!");
                }
            }
            else {
                cts.Cancel();
                DownloadLabel = "Download";
                IsDownloading = false;
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
