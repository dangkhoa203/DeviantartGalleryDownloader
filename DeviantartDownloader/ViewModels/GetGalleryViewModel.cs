using DeviantartDownloader.Command;
using DeviantartDownloader.Models;
using DeviantartDownloader.Models.Enum;
using DeviantartDownloader.Service;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;
using System.Windows;

namespace DeviantartDownloader.ViewModels {
    public class GetGalleryViewModel : DialogViewModel {
        public DeviantartService DeviantartService { get; set; }
        private string _loadingSearchFolder = "Search";
        public string LoadingSearchFolder {
            get { return _loadingSearchFolder; }
            set {
                _loadingSearchFolder = value;
                OnPropertyChanged(nameof(LoadingSearchFolder));
            }
        }

        private string _loadingSearchDeviant = "Add to list";
        public string LoadingSearchDeviant {
            get { return _loadingSearchDeviant; }
            set {
                _loadingSearchDeviant = value;
                OnPropertyChanged(nameof(LoadingSearchDeviant));
            }
        }

        private bool _isSearchable = true;
        public bool IsSearchable {
            get { return _isSearchable; }
            set {
                _isSearchable = value;
                OnPropertyChanged(nameof(IsSearchable));
            }
        }

        public CancellationTokenSource cts { get; set; } = new CancellationTokenSource();

        private string _selectedUsername = "Not selected";
        public string SelectedUsername {
            get { return _selectedUsername; }
            set {
                _selectedUsername = value;
                OnPropertyChanged(nameof(SelectedUsername));
            }
        }

        private GalleryFolder? _selectedFolder = null;
        public GalleryFolder? SelectedFolder {
            get { return _selectedFolder; }
            set {
                _selectedFolder = value;
                OnPropertyChanged(nameof(SelectedFolder));
            }
        }

        private ObservableCollection<GalleryFolder> _searchResultFolders=[];
        public ObservableCollection<GalleryFolder> SearchResultFolders {
            get { return _searchResultFolders; }
            set {
                _searchResultFolders = value;
                OnPropertyChanged(nameof(SearchResultFolders));
            }
        }
        private string _searchUserName="";
        public string SearchUserName {
            get { return _searchUserName; }
            set {
                _searchUserName = value;
                OnPropertyChanged(nameof(SearchUserName));
            }
        }
        private ObservableCollection<Deviant> _deviants=[];
        public ObservableCollection<Deviant> Deviants {
            get { return _deviants; }
            set {
                _deviants = value;
                OnPropertyChanged(nameof(Deviants));
            }
        }
        private bool _isSelectAll = false;
        public bool IsSelectAll { 
            get { return _isSelectAll; }
            set { _isSelectAll = value;
                foreach (var deviant in Deviants) {
                    deviant.IsSelected = value;
                }
                OnPropertyChanged(nameof(IsSelectAll));
               
            }  
        }
        public RelayCommand GetFolderCommand { get; set; }
        public RelayCommand GetDeviantCommand { get; set; }
        public RelayCommand RemoveDeviantFromListCommand { get; set; }
        public RelayCommand ClearListCommand { get; set; }
        public RelayCommand ResetUserCommand { get; set; }
        public RelayCommand SubmitToDownloadListCommand { get; set; }
        public RelayCommand CloseCommand { get; set; }
        public RelayCommand SelectAllArtCommand { get; set; }
        public RelayCommand SelectAllLiteratureCommand { get; set; }
        public RelayCommand SelectAllVideoCommand { get; set; }
        public GetGalleryViewModel(DeviantartService service) {
            DeviantartService = service;

            RemoveDeviantFromListCommand = new RelayCommand(o => {
                RemoveDeviantFromList(o as string ?? "");
            }, o => LoadingSearchDeviant != "Cancel");

            ClearListCommand = new RelayCommand(o => {
                var list=Deviants.Where(o => o.IsSelected).ToList();
                if (list.Count != Deviants.Count) {
                    list.ForEach(o => Deviants.Remove(o));
                }
                else {
                    Deviants.Clear();
                }
                IsSelectAll = false;
            }, o => Deviants.Where(o=>o.IsSelected).ToList().Count > 0 && LoadingSearchDeviant != "Cancel");

            GetFolderCommand = new RelayCommand(async o => {
                await GetFolder();
            }, o => true);

            GetDeviantCommand = new RelayCommand(async o => {
                await GetDeviants();
            }, o => SelectedUsername != "Not selected" && SelectedFolder != null);

            ResetUserCommand = new RelayCommand(o => { 
                ResetSearch(); 
            }, o => SelectedUsername != "Not selected");

            SubmitToDownloadListCommand = new RelayCommand(o => { 
                Success = true;
                Dialog.Close();
            }, o => Deviants.Count > 0);

            SelectAllArtCommand= new RelayCommand(o => {
                SelectDeviantType(DeviantType.Art);
            }, o => Deviants.Count > 0 && LoadingSearchDeviant != "Cancel");

            SelectAllLiteratureCommand= new RelayCommand(o => {
                SelectDeviantType(DeviantType.Literature);
            }, o => Deviants.Count > 0 && LoadingSearchDeviant != "Cancel");

            SelectAllVideoCommand= new RelayCommand(o => {
                SelectDeviantType(DeviantType.Video);
            }, o => Deviants.Count > 0 && LoadingSearchDeviant != "Cancel");

            CloseCommand = new RelayCommand(o => { 

            }, o => LoadingSearchDeviant != "Cancel");
        }
        private void SelectDeviantType(DeviantType deviantType) {
            var list = Deviants.Where(o => o.Type == deviantType).ToList();
            if (list.Count == Deviants.Count) {
                IsSelectAll = true;
            }
            else {
                foreach (var deviant in Deviants) {
                    if (deviant.Type == deviantType) {
                        deviant.IsSelected = true;
                    }
                }
            }
        }
        private void ResetSearch() {
            SelectedFolder = null;
            SelectedUsername = "Not selected";
            SearchResultFolders.Clear();
        }
        private void RemoveDeviantFromList(string Id) {
            var deviant = Deviants.FirstOrDefault(d => d.Id == Id);
            if (deviant != null) {
                Deviants.Remove(deviant);
            }
        }
        private async Task GetFolder() {
            if (LoadingSearchFolder != "Cancel") {
                if(SearchUserName == "") {
                    MessageBox.Show("Username empty!", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }
                if (SelectedUsername != SearchUserName) {
                    LoadingSearchFolder = "Cancel";
                    var f = await DeviantartService.GetFolders(SearchUserName, cts);
                    if (f.Count > 0) {
                        ResetSearch();
                        GalleryFolder allFolder = new("", "All", 0);
                        SelectedUsername = SearchUserName;
                        SearchResultFolders.Clear();
                        SearchResultFolders.Add(allFolder);
                        IsSearchable = false;
                        foreach (var a in f) {
                            SearchResultFolders.Add(a);
                        }
                        SelectedFolder = allFolder;
                        IsSearchable = true;
                    }
                    LoadingSearchFolder = "Search";
                }
            }
            else {
                LoadingSearchFolder = "Search";
                cts.Cancel();
                cts = new CancellationTokenSource();
            }
        }
        private async Task GetDeviants() {
            if (LoadingSearchDeviant != "Cancel") {
                LoadingSearchDeviant = "Cancel";
                var f = await DeviantartService.GetDeviants(SearchUserName, SelectedFolder.Id, cts);
                f = f.ToList();
                if (f.Count > 0) {
                    IsSearchable = false;
                    foreach (var a in f) {
                        if (!Deviants.Any(o => o.Id == a.Id)) {
                            Deviants.Add(a);
                        }
                    }
                    IsSearchable = true;
                }

                LoadingSearchDeviant = "Add to list";
            }
            else {
                LoadingSearchDeviant = "Add to list";
                cts.Cancel();
                cts = new CancellationTokenSource();
            }
        }
    }
}
