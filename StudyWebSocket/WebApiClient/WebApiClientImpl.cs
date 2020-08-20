using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using WebInterfaceLibrary;
using WebInterfaceLibrary.Schemas;

namespace WebApiClient
{
    class WebApiClientImpl : LifetimeEventsHostedService
    {
        static WebInterfaceLibrary.WebApiClient client = new WebInterfaceLibrary.WebApiClient()
        {
            BaseAddress = new Uri("http://localhost:80/")
        };

        public WebApiClientImpl(ILogger<WebApiClientImpl> logger, IHostApplicationLifetime appLifetime, IConfiguration configration) : base(logger, appLifetime, configration)
        {
        }

        protected override async void OnStarted()
        {
            //logger.LogInformation("{0} {1} {2}", configration.GetValue<string>("Option1"), configration.GetValue<int>("Option2"), configration.GetValue<Guid>("Option3"));

            base.OnStarted();

            HttpResponseMessage response = await client.GetAsync("Temporary_Listen_Addresses/v1.0/cpumodes/localhost");

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
            appLifetime.StopApplication();
        }

    }
}
