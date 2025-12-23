using DeviantartDownloader.Command;
using DeviantartDownloader.Service;
using DeviantartDownloader.Service.Interface;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Windows.Navigation;

namespace DeviantartDownloader.ViewModels
{
    public class MainWindowViewModel:ViewModel
    {
        private string _downloadPath;
        private readonly IDialogService _dialogService;
        public DeviantartClient Client { get; set; }
        public string DownloadPath {
            get { return _downloadPath; }
            set { _downloadPath = value;OnPropertyChanged("DownloadPath"); }
        }
        public RelayCommand GetDownloadPathCommand { get; set; }
        public RelayCommand GetGalleryDialogCommand { get; set; }
        public MainWindowViewModel(IDialogService service,DeviantartClient client) {
            Client = client;
            _downloadPath = string.Empty;
            _dialogService = service;
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
                    MessageBox.Show("Test");
                }
            }, o => true);

        }


    }
}
