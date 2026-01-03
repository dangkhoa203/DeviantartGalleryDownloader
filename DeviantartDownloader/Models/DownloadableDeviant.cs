using DeviantartDownloader.Models.Enum;
using System;
using System.Collections.Generic;
using System.Text;

namespace DeviantartDownloader.Models
{
    public class DownloadableDeviant
    {
        public Deviant Deviant { get; set; }
        public float Percent { get; set; }
        public DownloadStatus Status { get; set; }
        public string DownloadSpeed { get; set; }
        public DownloadableDeviant(Deviant deviant) {
            Deviant = deviant;
            Percent = 0;
            Status=DownloadStatus.Waiting;
            DownloadSpeed = "";
        }
        public bool? IsSelected { get; set; } = false;
    }
}
