using System;
using System.Collections.Generic;
using System.Text;
using DeviantartDownloader.Models;
namespace DeviantartDownloader.DTOs {
    internal class SearchDeviantResponse {
        public List<DeviantAPIContent>? results { get; set; }
        public int? next_offset { get; set; }
        public bool? has_more { get; set; }
        public string? error { set; get; }
        public string? error_description { get; set; }
    }
}
