using System;
using System.Data;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using WebSocketLibrary;
using WebSocketLibrary.Schemas;

namespace WebApiClient
{
    class Program
    {
        static WebSocketLibrary.WebApiClient client = new WebSocketLibrary.WebApiClient()
        {
            BaseAddress = new Uri("http://localhost:80/")
        };

        static async Task Main(string[] args)
        {
            HttpResponseMessage response = await client.GetAsync("Temporary_Listen_Addresses/cpumodes/localhost");
            if (response.IsSuccessStatusCode)
            {
                CpuMode result = await response.Content.ReadAsAsync<CpuMode>();

                Console.WriteLine(result);
            }
        }
    }
}
