using Hondarersoft.Hosting;
using Hondarersoft.WebInterface;
using Hondarersoft.WebInterface.Sample.Schemas;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;

namespace WebApiClient
{
    class WebApiClientImpl : LifetimeEventsHostedService
    {
        private readonly IWebApiClient _webApiClient = null;
        private readonly ICommonApiManager _commonApiManager = null;

        public WebApiClientImpl(ILogger<WebApiClientImpl> logger, IHostApplicationLifetime appLifetime, IConfiguration configration, IExitService exitService, IServiceProvider serviceProvider, IWebApiClient webApiClient, ICommonApiManager commonApiManager) : base(logger, appLifetime, configration, exitService)
        {
            _webApiClient = webApiClient;
            _commonApiManager = commonApiManager;
        }

        protected override async Task OnStartedAsync()
        {
            await base.OnStartedAsync();

            IWebInterface webInterace = _webApiClient as IWebInterface;
            webInterace.Hostname = "localhost";
            webInterace.PortNumber = 8001;

            await _commonApiManager.RegistInterface(webInterace).StartAsync();

            CommonApiRequest request = new CommonApiRequest()
            {
                Method = CommonApiMethods.GET,
                Path = "/api/v1/cpumodes/localhost"
            };

            CommonApiResponse response = await _commonApiManager.SendRequestAsync<CpuMode>(request);

            if (response.IsSuccess == true)
            {
                CpuMode cpuMode = response.ResponseBody as CpuMode;
                _logger.LogInformation("Success. response = {0}, {1}", cpuMode.Hostname, cpuMode.Modecode);
            }
            else
            {
                if (response.Error != null)
                {
                    _logger.LogError("Error. error.code = {0}, error.message = {1}", response.Error.Code, response.Error.Message);
                }
                else
                {
                    _logger.LogError("Error. No error information.");
                }
            }

            Console.ReadLine();

            _exitService.Requset(0);
        }
    }
}
