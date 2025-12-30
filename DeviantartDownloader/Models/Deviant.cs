using DeviantartDownloader.Models.Enum;
using System;
using System.Collections.Generic;
using System.Text;

namespace DeviantartDownloader.Models {
    public class Deviant {
        public string Deviationid { get; set; }
        public Author Author {  get; set; }
        public string? Url { get; set; }
        public string? Title { get; set; }
        public Content? Content { get; set; }
        public ICollection<Content>? Video { get; set; }
        public DeviantType Type { get; set; }
    }
}
