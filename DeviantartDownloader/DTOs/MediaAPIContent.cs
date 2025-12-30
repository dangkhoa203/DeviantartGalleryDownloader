using System;
using System.Collections.Generic;
using System.Text;

namespace DeviantartDownloader.DTOs
{
    public record MediaAPIContent
    {
        public string? src { get; set; }
        public int? filesize { get; set; }
    }
}
