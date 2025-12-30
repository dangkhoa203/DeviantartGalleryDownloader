using System;
using System.Collections.Generic;
using System.Text;

namespace DeviantartDownloader.DTOs
{
    public record AuthorAPIContent
    {
        public string userid { get; set; }
        public string username { get; set; }
    }
}
