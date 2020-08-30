using Hondarersoft.Hosting;
using Hondarersoft.WebInterface;
using Hondarersoft.WebInterface.Schemas;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;

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

        protected override async void OnStarted()
        {
            base.OnStarted();

            IWebInterface webInterace = _webSocketClient as IWebInterface;
            webInterace.Hostname = "localhost";
            webInterace.PortNumber = 8000;

            string webInterfaceIdentify = Guid.NewGuid().ToString();

            _commonApiManager.RegistInterface(webInterace, webInterfaceIdentify)
                .Start();

            CommonApiRequest request = new CommonApiRequest()
            {
                InterfaceIdentify = webInterfaceIdentify,
                Method = CommonApiMethods.GET,
                Path = "/api/v1/cpumodes/localhodst"
            };

            CommonApiResponse response = await _commonApiManager.SendRequestAsync(request);

            if (response.IsSuccess == true)
            {
                _logger.LogInformation("Success. response = {0}", response.Result);
            }
            if (response.Error != null)
            {
                _logger.LogError("Error. error.code = {0}, error.message = {1}", response.Error.Code, response.Error.Message);
            }
            else
            {
                _logger.LogError("Error. No error information.");
            }

            Console.ReadLine();

            _exitService.Requset(0);
        }
    }
}
