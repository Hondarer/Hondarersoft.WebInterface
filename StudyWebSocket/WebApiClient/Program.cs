using System;
using System.Data;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using WebSocketLibrary;

namespace WebApiClient
{
    class Program
    {
        static WebSocketLibrary.WebApiClient client = new WebSocketLibrary.WebApiClient();

        static async Task Main(string[] args)
        {
            client.BaseAddress = new Uri("http://localhost:80/");

            WebSocketLibrary.Schemas.CpuMode result = null;
            HttpResponseMessage response = await client.GetAsync("Temporary_Listen_Addresses/cpumodes/localhost");
            if (response.IsSuccessStatusCode)
            {
                //result = await response.Content.ReadAsStringAsync();
                result = await response.Content.ReadAsAsync<WebSocketLibrary.Schemas.CpuMode>();

                Console.WriteLine(result);
            }
        }
    }
}
