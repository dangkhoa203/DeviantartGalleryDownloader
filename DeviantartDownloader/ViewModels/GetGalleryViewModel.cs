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
        public DeviantartService client { get; set; }
        private string _loadingSearchFolder = "Search";
        public string loadingSearchFolder { 
            get { return _loadingSearchFolder; }
            set { _loadingSearchFolder = value; OnPropertyChanged(nameof(loadingSearchFolder)); } 
        }
        private string _loadingSearchDeviant = "Add to list";
        public string loadingSearchDeviant {
            get { return _loadingSearchDeviant; }
            set { _loadingSearchDeviant = value; OnPropertyChanged(nameof(loadingSearchDeviant)); }
        }
        private bool _isSearchable = true;
        public bool isSearchable { get { return _isSearchable; } set { _isSearchable = value;OnPropertyChanged(nameof(isSearchable)); } }

        public CancellationTokenSource cts { get; set; } = new CancellationTokenSource();

        private string _currentSearchUsername = "Not selected";
        public string selectedUsername { get { return _currentSearchUsername; } set { _currentSearchUsername = value;OnPropertyChanged("selectedUsername"); } }
        private Folder? _currentFolder = null;
        public Folder selectedFolder { get { return _currentFolder; } set { _currentFolder = value;OnPropertyChanged("selectedFolder"); } }
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
        public RelayCommand GetFolderCommand { get; set; }
        public RelayCommand GetDeviantCommand { get; set; }
        public RelayCommand RemoveDeviantFromListCommand { get; set; }
        public RelayCommand ResetUserCommand { get; set; }
        public RelayCommand SubmitToDownloadListCommand { get; set; }
        public GetGalleryViewModel(DeviantartService Client) {
            Folders = [];
            Deviant = [];
            client = Client;
            RemoveDeviantFromListCommand = new RelayCommand(o => {
                var d = Deviant.FirstOrDefault(d => d.Deviationid == o as string);
                if (d != null) {
                    Deviant.Remove(d);
                }
            }, o => true);
            GetFolderCommand = new RelayCommand(async o => {
                if (loadingSearchFolder != "Cancel") {
                    if (selectedUsername != UserName) {
                        loadingSearchFolder = "Cancel";
                        var f = await client.GetFolders(UserName, cts);
                        if (f.Count > 0) {
                            ResetSearch();
                            Folder allFolder = new() { Name = "All", Id = "", Size = f.Sum(o => o.Size) };
                            selectedUsername = UserName;
                            Folders.Clear();
                            Folders.Add(allFolder);
                            isSearchable = false;
                            foreach (var a in f) {
                                Folders.Add(a);
                            }
                            selectedFolder = allFolder;
                            isSearchable = true;
                        }
                        loadingSearchFolder = "Search";
                        MessageBox.Show("Search completed", "Information", MessageBoxButton.OK, MessageBoxImage.Exclamation);
                    }
                }
                else {
                    loadingSearchFolder = "Search";
                    cts.Cancel();
                    cts = new CancellationTokenSource();
                }
            }, o => true);
            GetDeviantCommand = new RelayCommand(async o => {
                if (loadingSearchDeviant != "Cancel") {
                    loadingSearchDeviant = "Cancel";
                    var f = await client.GetDeviants(UserName, selectedFolder.Id, cts);
                    f = f.ToList();
                    if (f.Count > 0) {
                        isSearchable = false;
                        foreach (var a in f) {
                            if (!Deviant.Any(o => o.Deviationid == a.Deviationid)) {
                                Deviant.Add(a);
                            }
                        }
                        isSearchable = true;
                    }

                    loadingSearchDeviant = "Add to list";
                }
                else {
                    loadingSearchDeviant = "Add to list";
                    cts.Cancel();
                    cts = new CancellationTokenSource();
                }
            }, o => selectedUsername != "Not selected" && selectedFolder !=null );
            ResetUserCommand = new RelayCommand(o => { ResetSearch(); }, o => selectedUsername != "Not selected");
            SubmitToDownloadListCommand = new RelayCommand(o => { Success = true; }, o => Deviant.Count > 0);
        }
        public bool Success { get; set; } = false;
        private void ResetSearch() {
            selectedFolder = null;
            selectedUsername = "Not selected";
            Folders.Clear();
        }
    }
}
