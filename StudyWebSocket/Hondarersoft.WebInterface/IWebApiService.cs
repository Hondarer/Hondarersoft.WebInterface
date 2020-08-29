using System;
using System.Collections.Generic;
using System.Net;
using System.Text;

namespace Hondarersoft.WebInterface
{
    public interface IWebApiService
    {
        public class WebApiRequestEventArgs : EventArgs
        {
            public HttpListenerRequest Request { get; }
            public HttpListenerResponse Response { get; }

            public WebApiRequestEventArgs(HttpListenerRequest request, HttpListenerResponse response)
            {
                Request = request;
                Response = response;
            }
        }

        public delegate void WebApiRequestHandler(object sender, WebApiRequestEventArgs e);
        public event WebApiRequestHandler WebApiRequest;

        public bool AllowCORS { get; set; }
    }
}
