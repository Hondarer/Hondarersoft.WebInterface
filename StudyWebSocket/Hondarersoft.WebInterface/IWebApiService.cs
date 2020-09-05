using System;
using System.Collections.Generic;
using System.Net;
using System.Text;

namespace Hondarersoft.WebInterface
{
    public interface IWebApiService : IWebInterfaceService
    {
        public class WebApiRequestEventArgs : EventArgs
        {
            public HttpListenerContext HttpListenerContext { get; }

            public WebApiRequestEventArgs(HttpListenerContext httpListenerContext)
            {
                HttpListenerContext = httpListenerContext;
            }
        }

        public delegate void WebApiRequestHandler(object sender, WebApiRequestEventArgs e);
        public event WebApiRequestHandler WebApiRequest;

        public bool AllowCORS { get; set; }
    }
}
