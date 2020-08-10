using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace WebSocketLibrary
{
    public class WebApiClient : HttpClient
    {
        public WebApiClient() : 
            base(new HttpClientHandler() { UseCookies = false }) // Cookie のやり取りをしている場合に、Cookie がキャッシュされないようにする。
        {

            DefaultRequestHeaders.Accept.Clear();
            DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        }

        public new Uri BaseAddress
        {
            get
            {
                return base.BaseAddress;
            }
            set
            {
                base.BaseAddress = value;

                // ずっと使用していると DNS 変更が反映されないということが起きうるので、 
                // HttpClient にコネクションを定期的にリサイクルするように指示をする。
                var sp = ServicePointManager.FindServicePoint(base.BaseAddress);
                sp.ConnectionLeaseTimeout = 60 * 1000; // 1 minute
            }
        }
    }
}
