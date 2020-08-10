using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace WebSocketLibrary
{
    public static class HttpContentExtensions
    {
        public static async Task<Tout> ReadAsAsync<Tout>(this HttpContent content)
        {
            return await JsonSerializer.DeserializeAsync<Tout>(await content.ReadAsStreamAsync());
        }
    }
}
