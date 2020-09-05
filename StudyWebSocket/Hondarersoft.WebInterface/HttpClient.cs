using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace Hondarersoft.WebInterface
{
    public class HttpClient : WebInterface, IHttpClient, IWebInteraceProxySetting
    {
        protected System.Net.Http.HttpClient Client { get; set; }

        #region IWebInteraceProxySetting Implements

        private bool _useDefaultProxy = false;

        public bool UseDefaultProxy
        {
            get
            {
                return _useDefaultProxy;
            }
            set
            {
                _useDefaultProxy = value;
                if (value == true)
                {
                    UseCustomProxy = false;
                }
            }
        }

        private bool _useCustomProxy = false;

        public bool UseCustomProxy
        {
            get
            {
                return _useCustomProxy;
            }
            set
            {
                _useCustomProxy = value;
                if (value == true)
                {
                    UseDefaultProxy = false;
                }
            }
        }

        public string ProxyUrl { get; set; } = null;

        public string ProxyAccount { get; set; } = null;

        public string ProxyPassword { get; set; } = null;

        #endregion

        public HttpClient(ILogger<HttpClient> logger) : base(logger)
        {
            // Cookie のやり取りをしている場合に、Cookie がキャッシュされないようにする。
            // Proxy はデフォルトでは明示的に OFF にする。

            HttpClientHandler httpClientHandler = new HttpClientHandler() { UseCookies = false };

            if (UseDefaultProxy == false)
            {
                if (UseCustomProxy == true)
                {
                    httpClientHandler.UseProxy = true;
                    httpClientHandler.Proxy = new WebProxy(ProxyUrl)
                    {
                        Credentials = new NetworkCredential(ProxyAccount, ProxyPassword)
                    };
                }
                else
                {
                    // 引数なしの WebProxy は、直接接続を提供する。
                    httpClientHandler.UseProxy = false;
                    httpClientHandler.Proxy = new WebProxy();
                }
            }

            Client = new System.Net.Http.HttpClient(httpClientHandler);

            Client.DefaultRequestHeaders.Accept.Clear();
            Client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        }

        private string baseAddress = null;

        public string BaseAddress
        {
            get
            {
                return baseAddress;
            }
            set
            {
                if (baseAddress != value)
                {
                    baseAddress = value;

                    Client.BaseAddress = new Uri(baseAddress);

                    // ずっと使用していると DNS 変更が反映されないということが起きうるので、 
                    // HttpClient にコネクションを定期的にリサイクルするように指示をする。
                    var sp = ServicePointManager.FindServicePoint(Client.BaseAddress);
                    sp.ConnectionLeaseTimeout = 60 * 1000; // 1 minute
                }
            }
        }

        public Task<HttpResponseMessage> GetAsync(string requestUri)
        {
            if ((string.IsNullOrEmpty(Hostname) == true) ||
                (PortNumber == 0))
            {
                throw new Exception("invalid endpoint parameter");
            }

            string ssl = string.Empty;
            if (UseSSL == true)
            {
                ssl = "s";
            }

            string tail = string.Empty;
            if (string.IsNullOrEmpty(BasePath) != true)
            {
                tail = "/";
            }

            BaseAddress = $"http{ssl}://{Hostname}:{PortNumber}/{BasePath}{tail}";

            return Client.GetAsync(requestUri);
        }

        public Task<HttpResponseMessage> GetAsync(string requestUri, TimeSpan timeout)
        {
            Client.Timeout = timeout;

            return GetAsync(requestUri);
        }
    }
}
