using DeviantartDownloader.Command;
using DeviantartDownloader.Models;
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
        public string DownloadPath {
            get { return _downloadPath; }
            set { _downloadPath = value;OnPropertyChanged("DownloadPath"); }
        }
        private ObservableCollection<DownloadableDeviant> _deviants;
        public ObservableCollection<DownloadableDeviant> Deviants {  get { return _deviants; } set { _deviants = value;OnPropertyChanged(nameof(Deviants)); } }
        public RelayCommand GetDownloadPathCommand { get; set; }
        public RelayCommand GetGalleryDialogCommand { get; set; }
        public RelayCommand RemoveDeviantFromListCommand { get; set; }
        public MainWindowViewModel(IDialogService service,DeviantartService client) {
            Client = client;
            _downloadPath = string.Empty;
            _dialogService = service;
            _deviants = [];
            RemoveDeviantFromListCommand = new RelayCommand(o => {
                var d = Deviants.FirstOrDefault(d => d.Deviant.Deviationid == o as string);
                if (d != null) {
                    Deviants.Remove(d);
                }
            }, o => true);
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
            }, o => true);
            GetGalleryDialogCommand= new RelayCommand(o => {
                
                var resultVm = _dialogService.ShowDialog<GetGalleryViewModel>(new GetGalleryViewModel(client)); 

                // 2. Transfer the data back to the Main Window
                if (resultVm.Success) {
                    foreach(var d in resultVm.Deviant) {
                        Deviants.Add(new(d));
                    }
                }
            }, o => true);

        }


    }
}
