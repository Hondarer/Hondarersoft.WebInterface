using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace Hondarersoft.WebInterface
{
    public class WebApiClient : WebInterfaceBase
    {
        protected HttpClient Client { get; set; }

        public WebApiClient()
        {

            // Cookie のやり取りをしている場合に、Cookie がキャッシュされないようにする。
            Client = new HttpClient(new HttpClientHandler() { UseCookies = false });

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
                (PortNumber == 0) ||
                (string.IsNullOrEmpty(BasePath) == true))
            {
                throw new Exception("invalid endpoint parameter");
            }

            string ssl = string.Empty;
            if (UseSSL == true)
            {
                ssl = "s";
            }

            BaseAddress = $"http{ssl}://{Hostname}:{PortNumber}/{BasePath}/";

            return Client.GetAsync(requestUri);
        }
    }
}
