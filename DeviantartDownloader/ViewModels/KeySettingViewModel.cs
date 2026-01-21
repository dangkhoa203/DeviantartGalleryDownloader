using DeviantartDownloader.Command;
using DeviantartDownloader.Service;
using MahApps.Metro.Controls.Dialogs;
using System;
using System.Collections.Generic;
using System.Security.Policy;
using System.Text;
using System.Windows.Media;

namespace DeviantartDownloader.ViewModels {
    public class KeySettingViewModel : DialogViewModel {
        private IDialogCoordinator _dialogCoordinator;
        private readonly DeviantartService _service;

        private string _code = "";
        public string Code {
            get {
                return _code;
            }
            set {
                _code = value;
                OnPropertyChanged(nameof(Code));
            }
        }

        private bool _isLoading = false;
        public bool IsLoading {
            get {
                return _isLoading;
            }
            set {
                _isLoading = value;
                OnPropertyChanged(nameof(IsLoading));
            }
        }

        private string _status="";
        public string Status {
            get {
                return _status;
            }
            set {
                _status=value;
                OnPropertyChanged(nameof(Status));
            }
        }

        private Brush _tileBrush;
        public Brush TitleBrush {
            get {
                return _tileBrush;
            }
            set {
                _tileBrush=value;
                OnPropertyChanged(nameof(TitleBrush));
            }
        }

        private bool _canDelete = false;
        public bool CanDelete {
            get {
                return _canDelete;
            }
            set {
                _canDelete=value;
                OnPropertyChanged(nameof(CanDelete));
            }
        }
        public RelayCommand AuthorizeCommand {
            get; set;
        }
        public RelayCommand ToAuthorizeCommand {
            get; set;
        }
        public RelayCommand DeleteUserKeyCommand {
            get; set;
        }
        public KeySettingViewModel(DeviantartService service, IDialogCoordinator dialogCoordinator) {
            _dialogCoordinator = dialogCoordinator;
            _service = service;
            CanDelete = _service.RefreshToken != null;
            Status = CanDelete ? "Currently using user key" :"Not using user key";
            TitleBrush = CanDelete ? Brushes.Green:Brushes.Red;

            ToAuthorizeCommand = new RelayCommand(async o => {
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo("https://www.deviantart.com/oauth2/authorize?response_type=code&client_id=60309&scope=browse&redirect_uri=https://dangkhoa203.github.io/GetDeviantartCode") { UseShellExecute = true });
            }, o => !IsLoading);

            AuthorizeCommand = new RelayCommand(async o => {
                IsLoading = true;
                var result = await _service.GetUserAccessToken(Code);
                if(result) {
                    var dialogResult = await _dialogCoordinator.ShowMessageAsync(this, "ALERT", "Authorize completed!", MessageDialogStyle.Affirmative);
                    Dialog.Close();
                }
                else {
                    await _dialogCoordinator.ShowMessageAsync(this, "ERROR", "Fail Authorize!", MessageDialogStyle.Affirmative);
                }
                IsLoading = false;
            }, o => Code.Length > 0 && !IsLoading);

            DeleteUserKeyCommand = new RelayCommand(async o => {
                _service.RefreshToken = null;
                _service.KeyTime = null;
                CanDelete = false;
                Status =  "Not using user key";
                TitleBrush = Brushes.Red;
            }, o => CanDelete && !IsLoading);
        }
    }
}
