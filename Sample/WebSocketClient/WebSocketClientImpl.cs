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
        private readonly ICommonApiService _commonApiService = null;

        public WebSocketClientImpl(ILogger<WebSocketClientImpl> logger, IHostApplicationLifetime appLifetime, IConfiguration configuration, IExitService exitService, IWebSocketClient webSocketClient, ICommonApiService commonApiService) : base(logger, appLifetime, configuration, exitService)
        {
            _webSocketClient = webSocketClient;
            _commonApiService = commonApiService;
        }

        protected override async Task OnStartedAsync()
        {
            await base.OnStartedAsync();

            await _commonApiService.RegistInterface(_webSocketClient.LoadConfiguration(_configuration))
                .StartAsync();

            CommonApiRequest request = new CommonApiRequest()
            {
                Method = CommonApiMethods.GET,
                Path = "/api/v1/cpumodes/localhost"
            };

            CommonApiResponse response = await _commonApiService.SendRequestAsync<CpuMode>(request);

            if (response.IsSuccess == true)
            {
                CpuMode cpuMode = response.ResponseBody as CpuMode;
                _logger.LogInformation("Success. response = {0}, {1}", cpuMode.Hostname, cpuMode.Modecode);
            }
            else
            {
                if (response.Error != null)
                {
                    _logger.LogError("Error. error.code = {0}, error.message = {1}, error.data = {2}", response.Error.Code, response.Error.Message, response.Error.Data);
                }
                else
                {
                    _logger.LogError("Error. No error information.");
                }
            }

            Console.ReadLine();

            _exitService.Request(0);
        }
    }
}
