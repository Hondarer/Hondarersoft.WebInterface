using System;
using System.Data;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using WebInterfaceLibrary;
using WebInterfaceLibrary.Schemas;

namespace WebApiClient
{
    class Program
    {
        static WebInterfaceLibrary.WebApiClient client = new WebInterfaceLibrary.WebApiClient()
        {
            BaseAddress = new Uri("http://localhost:80/")
        };

        static async Task Main(string[] args)
        {
            HttpResponseMessage response = await client.GetAsync("Temporary_Listen_Addresses/v1.0/cpumodes/localhost");
            if (response.IsSuccessStatusCode)
            {
                CpuMode result = await response.Content.ReadAsAsync<CpuMode>();

                Console.WriteLine(result.Hostname);
            }
            else
            {
                // TODO: 例外のハンドリングが甘い
                // (中まで行って帰ってきたら Error 型になるが、503とか、行きつかないエラーだとjsonになっていない)

                Error error = await response.Content.ReadAsAsync<Error>();

                Console.WriteLine(error.Message);
            }
        }
    }
}
