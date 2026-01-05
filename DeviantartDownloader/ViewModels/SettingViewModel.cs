using DeviantartDownloader.Command;
using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;

namespace DeviantartDownloader.ViewModels {
    public class SettingViewModel : DialogViewModel {
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
        public SettingViewModel(string headerString, int queueLimit) {
            _headerString = headerString;
            _queueLimit = queueLimit.ToString();
            SaveCommand = new RelayCommand(o => {
                var Result = MessageBox.Show("Are you sure you want to save?", "Saving cookie", MessageBoxButton.YesNo, MessageBoxImage.Question);
                if(Result == MessageBoxResult.Yes) {
                    Success = true;
                    Dialog.Close();
                }
            }, o => QueueLimit != "");
        }
    }
}
