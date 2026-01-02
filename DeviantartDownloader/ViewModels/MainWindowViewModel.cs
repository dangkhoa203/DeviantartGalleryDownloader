using DeviantartDownloader.Command;
using DeviantartDownloader.Models;
using DeviantartDownloader.Models.Enum;
using DeviantartDownloader.Service;
using DeviantartDownloader.Service.Interface;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;
using System.Windows;
using System.Windows.Navigation;

namespace DeviantartDownloader.ViewModels
{
    public class MainWindowViewModel:ViewModel
    {
        private string _downloadPath;
        private readonly IDialogService _dialogService;
        public DeviantartService Client { get; set; }
        public CancellationTokenSource cts { get; set; } = new CancellationTokenSource();

        public string DownloadPath {
            get { return _downloadPath; }
            set { _downloadPath = value;OnPropertyChanged("DownloadPath"); }
        }
        private ObservableCollection<DownloadableDeviant> _deviants;
        public ObservableCollection<DownloadableDeviant> Deviants {  get { return _deviants; } set { _deviants = value;OnPropertyChanged(nameof(Deviants)); } }
        public RelayCommand GetDownloadPathCommand { get; set; }
        public RelayCommand GetGalleryDialogCommand { get; set; }
        public RelayCommand RemoveDeviantFromListCommand { get; set; }
        public RelayCommand ClearListCommand { get; set; }
        public RelayCommand DonwloadDeviantCommand { get; set; }
        private bool _isDownloading;
        public bool isDownloading { get { return _isDownloading; } set { _isDownloading = value;OnPropertyChanged(nameof(isDownloading)); } }
        public MainWindowViewModel(IDialogService service, DeviantartService client) {
            Client = client;
            isDownloading = false;
            _downloadPath = string.Empty;
            _dialogService = service;
            _deviants = [];
            RemoveDeviantFromListCommand = new RelayCommand(o => {
                var d = Deviants.FirstOrDefault(d => d.Deviant.Deviationid == o as string);
                if (d != null) {
                    Deviants.Remove(d);
                }
            }, o => !isDownloading);
            GetDownloadPathCommand = new RelayCommand(o => {
                var folderDialog = new OpenFolderDialog {
                    Title = "Select Folder",
                    InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.Desktop)
                };

                bool? result = folderDialog.ShowDialog();
                if (result == true) {
                    string folderName = folderDialog.FolderName;
                    DownloadPath = folderName;
                }
            }, o => !isDownloading);
            GetGalleryDialogCommand = new RelayCommand(o => {

                var resultVm = _dialogService.ShowDialog<GetGalleryViewModel>(new GetGalleryViewModel(client));

                // 2. Transfer the data back to the Main Window
                if (resultVm.Success) {
                    foreach (var d in resultVm.Deviant) {
                        Deviants.Add(new(d));
                    }
                }
            }, o => !isDownloading);
            ClearListCommand = new RelayCommand(o => { Deviants.Clear(); }, o => !isDownloading && Deviants.Count>0);
            DonwloadDeviantCommand = new RelayCommand(async o => {
                isDownloading = true;
                foreach (var d in Deviants) {
                    if (d.Status != DownloadStatus.Completed) {
                        await Client.DonwloadDeviant(d, cts, DownloadPath);
                    }
                }
                var test = Deviants.ToList();
                Deviants.Clear();
                foreach (var d in test) {
                    Deviants.Add(d);
                }
                isDownloading= false;
               
            }, o => { return Deviants.Where(o=>o.Status!=DownloadStatus.Completed).ToList().Count > 0 && DownloadPath.Count() > 0 && !isDownloading; });
        }

        private void clearStatus() {
            foreach(var d in Deviants) {
                d.Status = DownloadStatus.Waiting;
                d.Percent = 0;
            }
        }
    }
}
