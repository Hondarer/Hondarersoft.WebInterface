using Hondarersoft.Hosting;
using Hondarersoft.WebInterface;
using Hondarersoft.WebInterface.Sample.Schemas;
using Hondarersoft.WebInterface.Schemas;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Net.Http;

namespace WebApiClient
{
    class WebApiClientImpl : LifetimeEventsHostedService
    {
        static Hondarersoft.WebInterface.WebApiClient _client = new Hondarersoft.WebInterface.WebApiClient()
        {
            Hostname="localhost",
            PortNumber=8001,
            BasePath="api/v1"
        };

        public WebApiClientImpl(ILogger<WebApiClientImpl> logger, IHostApplicationLifetime appLifetime, IConfiguration configration) : base(logger, appLifetime, configration)
        {
        }

        protected override async void OnStarted()
        {
            base.OnStarted();

            HttpResponseMessage response = await _client.GetAsync("cpumodes/localhost");

            if (response.IsSuccessStatusCode)
            {
                CpuMode result = await response.Content.ReadAsAsync<CpuMode>();

                Console.WriteLine($"target={result.Hostname}, mode={result.Modecode}");
            }
            else
            {
                // TODO: 例外のハンドリングが甘い
                // (中まで行って帰ってきたら Error 型になるが、503とか、行きつかないエラーだとjsonになっていないとか)

                Error error = await response.Content.ReadAsAsync<Error>();

                Console.WriteLine(error.Message);
            }

            Console.WriteLine("Press any key");
            Console.ReadLine();

            //Environment.ExitCode = 123;
            _appLifetime.StopApplication();
        }

    }
}
