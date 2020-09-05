using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Net;
using System.Text;

namespace Hondarersoft.WebInterface
{
    public interface IHttpService : IWebInterfaceService
    {
        public class HttpRequestEventArgs : EventArgs
        {
            public HttpListenerContext HttpListenerContext { get; }

            public HttpRequestEventArgs(HttpListenerContext httpListenerContext)
            {
                HttpListenerContext = httpListenerContext;
            }
        }

        public delegate void HttpRequestHandler(object sender, HttpRequestEventArgs e);
        public event HttpRequestHandler HttpRequest;

        public bool AllowCORS { get; set; }

        public new IHttpService LoadConfiguration(IConfiguration configurationRoot);
    }
}
