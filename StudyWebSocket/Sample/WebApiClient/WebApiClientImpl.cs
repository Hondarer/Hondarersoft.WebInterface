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
        private readonly IWebApiClient _webApiClient = null;

        public WebApiClientImpl(ILogger<WebApiClientImpl> logger, IHostApplicationLifetime appLifetime, IConfiguration configration, IExitService exitService, IWebApiClient webApiClient) : base(logger, appLifetime, configration, exitService)
        {
            _webApiClient = webApiClient;
        }

        protected override async void OnStarted()
        {
            base.OnStarted();

            IWebInterface webInterace = _webApiClient as IWebInterface;
            webInterace.Hostname = "localhost";
            webInterace.PortNumber = 8001;
            webInterace.BasePath = "api/v1";

            HttpResponseMessage response = await _webApiClient.GetAsync("cpumodes/localhost");

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

            _exitService.Requset(0);
        }

    }
}
