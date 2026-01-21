using System;
using System.Collections.Generic;
using System.Text;

namespace DeviantartDownloader.Models
{
    public class ClientToken
    {
        public string Token {
            get; set;
        }
        public DateTime KeyTime {
            get; set;
        }

        public ClientToken(string token, DateTime time) {
            Token = token;
            KeyTime = time;
        }
    }
}
