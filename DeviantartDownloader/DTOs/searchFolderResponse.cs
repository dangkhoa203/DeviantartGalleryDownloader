using System;
using System.Collections.Generic;
using System.Text;
using DeviantartDownloader.Models;
namespace DeviantartDownloader.DTOs
{
    class searchFolderResponse
    {
        public List<FolderAPIContent>? results {  get; set; }
        public int? next_offset { get; set; }
        public bool? has_more { get; set; }
        public string? error { set; get; }
        public string? error_description { get;set; }
        public int? size { get; set; }
    }
}
