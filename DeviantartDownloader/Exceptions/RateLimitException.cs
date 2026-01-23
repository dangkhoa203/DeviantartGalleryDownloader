using System;
using System.Collections.Generic;
using System.Text;

namespace DeviantartDownloader.Exceptions {
    internal class RateLimitException : Exception {
        public RateLimitException() {
        }
        public RateLimitException(string message)
            : base(message) {
        }

        public RateLimitException(string message, Exception innerException)
            : base(message, innerException) {

        }
    }
}