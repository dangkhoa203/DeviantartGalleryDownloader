using DeviantartDownloader.Command;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Windows.Navigation;

namespace DeviantartDownloader.ViewModels
{
    class MainWindowViewModel:ViewModel
    {
        private string _downloadPath;
        public string DownloadPath {
            get { return _downloadPath; }
            set { _downloadPath = value;OnPropertyChanged("DownloadPath"); }
        }
        public RelayCommand GetDownloadPathCommand { get; set; }
        public MainWindowViewModel() {
            _downloadPath = string.Empty;
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
        }


    }
}
