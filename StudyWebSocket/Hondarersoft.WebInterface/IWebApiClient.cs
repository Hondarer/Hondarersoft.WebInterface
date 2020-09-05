using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace Hondarersoft.WebInterface
{
    public interface IWebApiClient : IWebInterface
    {
        public Task<HttpResponseMessage> GetAsync(string requestUri);
        public Task<HttpResponseMessage> GetAsync(string requestUri, TimeSpan timeout);
    }
}
