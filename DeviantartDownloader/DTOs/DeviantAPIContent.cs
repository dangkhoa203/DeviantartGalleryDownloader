using DeviantartDownloader.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace DeviantartDownloader.DTOs
{
    public record DeviantAPIContent
    {
        public string deviationid { get; set; }
        public AuthorAPIContent author { get; set; }
        public string? url { get; set; }
        public string? title { get; set; }
        public Content? content { get; set; }
        public ICollection<Content>? video { get; set; }
        public string? excerpt { get; set; }
    }
}
