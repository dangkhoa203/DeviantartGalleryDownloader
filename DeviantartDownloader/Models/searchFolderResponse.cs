using System;
using System.Collections.Generic;
using System.Text;

namespace DeviantartDownloader.Models
{
    class searchFolderResponse
    {
        public List<Folder>? results {  get; set; }
        public int? next_offset { get; set; }
        public bool? has_more { get; set; }
        public string? error { set; get; }
        public string? error_description { get;set; }
    }
}
