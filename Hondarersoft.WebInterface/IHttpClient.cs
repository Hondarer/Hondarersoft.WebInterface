﻿using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace Hondarersoft.WebInterface
{
    public interface IHttpClient : IWebInterface
    {
        public Task<HttpResponseMessage> GetAsync(string requestUri);
        public Task<HttpResponseMessage> GetAsync(string requestUri, TimeSpan timeout);
        public Task<HttpResponseMessage> PostAsync(string requestUri, HttpContent content);
        public Task<HttpResponseMessage> PostAsync(string requestUri, HttpContent content, TimeSpan timeout);
    }
}
