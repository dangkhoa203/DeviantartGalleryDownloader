using System;
using System.Collections.Generic;
using System.Text;

namespace DeviantartDownloader.Models {
    public class AppSetting {
        public string HeaderString {
            get; set;
        } = "";
        public int QueueLimit {
            get; set;
        } = 3;
        public int UserKeySearchFolderWaitTime {
            get; set;
        } = 1;
        public int UserKeySearchDeviantWaitTime {
            get; set;
        } = 1;
        public int UserKeyDownloadDeviantWaitTime {
            get; set;
        } = 2;
        public bool DownloadDescription {
            get; set;
        } = false;
        public bool DownloadDescriptionOnly {
            get; set;
        } = false;
        public bool IgnoreEmptyDescription {
            get; set;
        } = true;
        public bool UseCustomStyle {
            get; set;
        } = false;
        public string CustomStyle {
            get; set;
        } = "";
    }
}
