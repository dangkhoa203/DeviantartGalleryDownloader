using DeviantartDownloader.Command;
using FluentValidation;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Net;
using System.Text;
using System.Windows;

namespace DeviantartDownloader.ViewModels {
    public class CookieSettingViewModel:DialogViewModel {
        public bool Success { get; set; }
        private string _headerString="";
        public string HeaderString { 
            get { return _headerString; }
            set { _headerString = value; }
        }
        public RelayCommand SaveCommand { get; set; }
        public CookieSettingViewModel(string headerString) {
            _headerString = headerString;
            SaveCommand = new RelayCommand(o => {
                var Result = MessageBox.Show("Save","Are you sure you want to save?",MessageBoxButton.YesNo,MessageBoxImage.Question);
                if (Result == MessageBoxResult.Yes) {
                    Success = true;
                    Dialog.Close();
                }
            },o=>HeaderString.Count()>0);
        }
    }
}
