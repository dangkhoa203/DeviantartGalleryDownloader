using System;
using System.Collections.Generic;
using System.Text;

namespace DeviantartDownloader.Models
{
    public class UserToken
    {
        public string Token { get; set; }
        public string RefreshToken { get; set; }
        public DateTime KeyTime { get; set; }

        public UserToken(string token, string refresh, DateTime time) {
            Token=token;
            RefreshToken=refresh;
            KeyTime=time;
        }
    }
}
