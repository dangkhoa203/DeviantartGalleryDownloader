using DeviantartDownloader.Models.Enum;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;

namespace DeviantartDownloader.Models {
    public class Deviant: ObservableObject {
        private bool _isSeleted = false;
        public bool IsSelected { 
            get { return _isSeleted; } 
            set { _isSeleted = value;
                OnPropertyChanged(nameof(IsSelected));
            } }
        public string Id { get; set; }
        public Author? Author {  get; set; }
        public string? Url { get; set; }
        public string? Title { get; set; }
        public MediaContent? Content { get; set; }
        public ICollection<MediaContent>? Video { get; set; }
        public DeviantType Type { get; set; }
        public bool Donwloadable { get; set; }
        public DateTime PublishDate { get; set; }
        
    }
}
