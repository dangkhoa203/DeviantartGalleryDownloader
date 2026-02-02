using System;
using System.Collections.Generic;
using System.Text;

namespace DeviantartDownloader.Models.Enum
{
    public enum DownloadStatus
    {
        Waiting=0,
        Downloading=1,
        Completed=2,
        Fail=3,
        Canceled = 4,
        Rate_Limited=5,
        Tier_Locked=6
    }
}
