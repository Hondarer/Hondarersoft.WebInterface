using System;
using System.Collections.Generic;
using System.Net;
using System.Text;

namespace Hondarersoft.WebInterface
{
    public class HttpRequestEventArgs : EventArgs
    {
        public HttpListenerContext HttpListenerContext { get; }

        public HttpRequestEventArgs(HttpListenerContext httpListenerContext)
        {
            HttpListenerContext = httpListenerContext;
        }
    }
}
