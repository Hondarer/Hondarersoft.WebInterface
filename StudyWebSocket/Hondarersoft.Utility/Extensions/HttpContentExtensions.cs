using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;

namespace Hondarersoft.Utility.Extensions
{
    public static class HttpContentExtensions
    {
        public static async Task<Tout> ReadAsJsonAsync<Tout>(this HttpContent content)
        {
            return await JsonSerializer.DeserializeAsync<Tout>(await content.ReadAsStreamAsync());
        }
    }
}
