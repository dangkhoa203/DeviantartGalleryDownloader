using DeviantartDownloader.Command;
using MahApps.Metro.Controls.Dialogs;
using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;

namespace DeviantartDownloader.ViewModels {
    public class SettingViewModel : DialogViewModel {
        private IDialogCoordinator _dialogCoordinator;
        private string _headerString = "";
        public string HeaderString {
            get {
                return _headerString;
            }
            set {
                _headerString = value;
            }
        }
        public string _queueLimit = "";
        public string QueueLimit {
            get {
                return _queueLimit;
            }
            set {
                _queueLimit = value;
            }
        }
        public RelayCommand SaveCommand {
            get; set;
        }
        public SettingViewModel(string headerString, int queueLimit, IDialogCoordinator dialogCoordinator) {
            _dialogCoordinator= dialogCoordinator;
            _headerString = headerString;
            _queueLimit = queueLimit.ToString();
            SaveCommand = new RelayCommand(async o => {
                var Result = await _dialogCoordinator.ShowMessageAsync(this, "ALERT", "Are you sure you want to save?", MessageDialogStyle.AffirmativeAndNegative);
                if(Result == MessageDialogResult.Affirmative) {
                    Success = true;
                    Dialog.Close();
                }
            }, o => QueueLimit != "");
        }
    }
}
