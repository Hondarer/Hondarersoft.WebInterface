using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace WebInterfaceLibrary
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

        public Uri BaseAddress
        {
            get
            {
                return Client.BaseAddress;
            }
            set
            {
                Client.BaseAddress = value;

                // ずっと使用していると DNS 変更が反映されないということが起きうるので、 
                // HttpClient にコネクションを定期的にリサイクルするように指示をする。
                var sp = ServicePointManager.FindServicePoint(Client.BaseAddress);
                sp.ConnectionLeaseTimeout = 60 * 1000; // 1 minute
            }
        }

        public Task<HttpResponseMessage> GetAsync(string requestUri)
        {
            return Client.GetAsync(requestUri);
        }
    }
}
