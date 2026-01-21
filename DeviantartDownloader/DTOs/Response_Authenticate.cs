using System;
using System.Collections.Generic;
using System.Text;

namespace DeviantartDownloader.DTOs
{
    public class Response_Authenticate
    {
        public string access_token {  get; set; }
        public string? refresh_token{ get; set; }
    }
}
