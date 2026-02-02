using DeviantartDownloader.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace DeviantartDownloader.DTOs
{
    public record Content_DeviantAPI
    {
        public string deviationid { get; set; }
        public Content_AuthorAPI author { get; set; }
        public string? url { get; set; }
        public string? title { get; set; }
        public Content_MediaAPI? content { get; set; }
        public ICollection<Content_MediaAPI>? videos { get; set; }
        public string? excerpt { get; set; }
        public bool? is_downloadable { get; set; }
        public string? published_time   { get; set; }
        public string? tier_access {get; set; }
    }
}
