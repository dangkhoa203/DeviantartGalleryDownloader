using DeviantartDownloader.Models.Enum;
using System;
using System.Collections.Generic;
using System.Text;

namespace DeviantartDownloader.Models
{
    public class DownloadableDeviant:ObservableObject
    {
        private float _percent;
        public float Percent {
            get { return _percent; }
            set {
                _percent = value;
                OnPropertyChanged(nameof(Percent));
                OnPropertyChanged(nameof(DisplayPercent));
            }
        }
        public DownloadStatus _status;
        public DownloadStatus Status {
            get { return _status; }
            set {
                _status = value;
                OnPropertyChanged(nameof(Status));
            }
        }

        private string _downloadSpeed;
        public string DownloadSpeed {
            get { return _downloadSpeed; }
            set {
                _downloadSpeed = value;
                OnPropertyChanged(nameof(DownloadSpeed));
            }
        }
        public string DisplayPercent {
            get { return $"{_percent:0.##}%"; }
        }

        public Deviant Deviant { get; set; }
        
      
       
        public DownloadableDeviant(Deviant deviant) {
            Deviant = deviant;
            _percent = 0;
            _status = DownloadStatus.Waiting;
            _downloadSpeed = "";
        }
        private bool _isSeleted = false;
        public bool IsSelected {
            get {
                return _isSeleted;
            }
            set {
                _isSeleted = value;
                OnPropertyChanged(nameof(IsSelected));
            }
        }
    }
}
