using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;

namespace Hondarersoft.WebInterface
{
    public static class HttpContentExtensions
    {
        public static async Task<T> ReadAsJsonAsync<T>(this HttpContent content)
        {
            return await JsonSerializer.DeserializeAsync<T>(await content.ReadAsStreamAsync());
        }
    }
}
