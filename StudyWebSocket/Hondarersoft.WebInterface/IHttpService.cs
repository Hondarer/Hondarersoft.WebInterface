using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Net;
using System.Text;

namespace Hondarersoft.WebInterface
{
    public interface IHttpService : IWebInterfaceService
    {

        public event EventHandler<HttpRequestEventArgs> HttpRequest;

        public bool AllowCORS { get; set; }

        public new IHttpService LoadConfiguration(IConfiguration configurationRoot);
    }
}
