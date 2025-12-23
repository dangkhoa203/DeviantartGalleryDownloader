using DeviantartDownloader.Command;
using DeviantartDownloader.Models;
using DeviantartDownloader.Service;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;
using System.Windows;

namespace DeviantartDownloader.ViewModels {
    public class GetGalleryViewModel : ViewModel {
        public DeviantartClient client { get; set; }
        private string _loadingSearch = "Search";
        public string loadingSearch { 
            get { return _loadingSearch; }
            set { _loadingSearch = value; OnPropertyChanged("LoadingSearch"); } 
        }
        private bool _isSearchable = true;
        public bool isSearchable { get { return _isSearchable; } set { _isSearchable = value;OnPropertyChanged("searchable"); } }

        public CancellationTokenSource cts { get; set; } = new CancellationTokenSource();

        private string _currentSearchUsername = "Not selected";
        public string selectedUsername { get { return _currentSearchUsername; } set { _currentSearchUsername = value;OnPropertyChanged("selectedUsername"); } }
        public string selfolderid { get; set; }
        private ObservableCollection<Folder> _folders;
        public ObservableCollection<Folder> Folders {
            get { return _folders; }
            set {
                _folders = value;
                OnPropertyChanged("cbFolder");
            }
        }
        private string _userName;
        public string UserName {
            get { return _userName; }
            set {
                _userName = value;
                OnPropertyChanged("Username");
            }
        }
        private ObservableCollection<Deviant> _Deviant;
        public ObservableCollection<Deviant> Deviant {
            get { return _Deviant; }
            set {
                _Deviant = value;
                OnPropertyChanged("dgDeviant");
            }
        }
        public RelayCommand TestCommand { get; set; }
        public RelayCommand GetDeviantCommand { get; set; }
        public RelayCommand ResetUserCommand { get; set; }
        public GetGalleryViewModel(DeviantartClient Client) {
            Folders = [];
            Deviant = [];
            client = Client;
            TestCommand = new RelayCommand(async o => {
                if (loadingSearch != "Cancel") {
                    loadingSearch = "Cancel";
                    var f = await client.GetFolders(UserName, cts);
                   
                    if (f.Count > 0) {
                        Folders.Add(new() { name = "All", folderid = "" });
                        selectedUsername = UserName;
                        Folders.Clear();
                        foreach (var a in f) {
                            isSearchable = false;
                            Folders.Add(a);
                        }
                    }
                    
                    loadingSearch = "Search";
                }
                else {
                    loadingSearch = "Search";
                    cts.Cancel();
                    cts = new CancellationTokenSource();
                }
            }, o => true);
            GetDeviantCommand = new RelayCommand(async o => {
                if (loadingSearch != "Cancel") {
                    loadingSearch = "Cancel";
                    var f = await client.GetDeviants(UserName,selfolderid, cts);

                    if (f.Count > 0) {   
                        foreach (var a in f) {
                            isSearchable = false;
                            Deviant.Add(a);
                        }
                    }

                    loadingSearch = "Search";
                }
                else {
                    loadingSearch = "Search";
                    cts.Cancel();
                    cts = new CancellationTokenSource();
                }
            }, o => selectedUsername!= "Not selected" && selfolderid!="");
            ResetUserCommand = new RelayCommand( o => { ResetSearch(); }, o => selectedUsername!= "Not selected");
        }
        public bool Success { get; set; } = false;
        private void ResetSearch() {
            selectedUsername = "Not selected";
            Folders.Clear();
        }
    }
}
