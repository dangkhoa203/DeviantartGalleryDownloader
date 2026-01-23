using DeviantartDownloader.Command;
using DeviantartDownloader.Models.Enum;
using DeviantartDownloader.Service;
using MahApps.Metro.Controls.Dialogs;
using System;
using System.Collections.Generic;
using System.Text;

namespace DeviantartDownloader.ViewModels {
    public class DownloadModeViewModel:DialogViewModel {
        private IDialogCoordinator _dialogCoordinator;
        public List<TypeItem> TypeModes {
            get;
        } = [
            new TypeItem("All",null),
            new TypeItem("Only Art",DeviantType.Art),
            new TypeItem("Only Literature",DeviantType.Literature),
            new TypeItem("Only Video",DeviantType.Video),
        ];
        public List<StatusItem> StatusModes {
            get;
        } = [
            new StatusItem("All",null),
            new StatusItem("Only Waiting",DownloadStatus.Waiting),
            new StatusItem("Only Fail",DownloadStatus.Fail),
            new StatusItem("Only Rate_Limited",DownloadStatus.Rate_Limited),
        ];

        private DeviantType? _selectedType;
        public DeviantType? SelectedType {
            get {
                return _selectedType;
            }
            set {
                _selectedType = value;
                OnPropertyChanged(nameof(SelectedType));
            }
        }

        private DownloadStatus? _selectedStatus;
        public DownloadStatus? SelectedStatus {
            get {
                return _selectedStatus;
            }
            set {
                _selectedStatus = value;
                OnPropertyChanged(nameof(SelectedStatus));
            }
        }
        public RelayCommand SaveCommand {
            get; set;
        }
        public DownloadModeViewModel(IDialogCoordinator dialogCoordinator,DeviantType? type,DownloadStatus? status) {
             _selectedType=type;
            _selectedStatus=status;
            _dialogCoordinator = dialogCoordinator;
            SaveCommand = new RelayCommand(async o => {
                var Result = await _dialogCoordinator.ShowMessageAsync(this, "ALERT", "Are you sure you want to save?", MessageDialogStyle.AffirmativeAndNegative);
                if(Result == MessageDialogResult.Affirmative) {
                    Success = true;
                    Dialog.Close();
                }
            }, o =>true);
        }
    }
    public record TypeItem(string Display, DeviantType? Type);
    public record StatusItem(string Display, DownloadStatus? Status);
}
