using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;

namespace DeviantartDownloader.ViewModels {
    public class DialogViewModel:ViewModel {
        public Window? Dialog { get; set; } = null;
        public bool Success { get; set; } = true;
    }
}
