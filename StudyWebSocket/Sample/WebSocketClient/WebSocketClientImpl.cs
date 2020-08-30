using Hondarersoft.Hosting;
using Hondarersoft.WebInterface;
using Hondarersoft.WebInterface.Sample.Schemas;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;

namespace WebSocketClient
{
    class WebSocketClientImpl : LifetimeEventsHostedService
    {
        private readonly IWebSocketClient _webSocketClient = null;
        private readonly ICommonApiManager _commonApiManager = null;

        public WebSocketClientImpl(ILogger<WebSocketClientImpl> logger, IHostApplicationLifetime appLifetime, IConfiguration configration, IExitService exitService, IWebSocketClient webSocketClient, ICommonApiManager commonApiManager) : base(logger, appLifetime, configration, exitService)
        {
            _webSocketClient = webSocketClient;
            _commonApiManager = commonApiManager;
        }

        protected override async Task OnStartedAsync()
        {
            await base.OnStartedAsync();

            IWebInterface webInterace = _webSocketClient as IWebInterface;
            webInterace.Hostname = "localhost";
            webInterace.PortNumber = 8000;

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
