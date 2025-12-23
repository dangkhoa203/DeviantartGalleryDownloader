using System;
using System.Collections.Generic;
using System.Text;

namespace DeviantartDownloader.Models {
    public class Deviant {
        public string deviationid { get; set; }
        public Author author {  get; set; }
        public string? url { get; set; }
        public string? title { get; set; }
        public Content? content { get; set; }
        public ICollection<Content> video { get; set; }
        public string? excerpt { get; set; }
    }
}
