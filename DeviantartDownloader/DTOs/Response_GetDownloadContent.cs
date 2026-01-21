using System;
using System.Collections.Generic;
using System.Text;

namespace DeviantartDownloader.DTOs {
    internal class Response_GetDownloadContent {
        public string? src {  get; set; }
        public string? filename { get; set; }
        public int? filesize { get; set; }
        public string? error { set; get; }
        public string? error_description { get; set; }

    }
}
