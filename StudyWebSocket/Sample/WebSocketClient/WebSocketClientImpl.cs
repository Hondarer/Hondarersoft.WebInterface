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

        public WebSocketClientImpl(ILogger<WebSocketClientImpl> logger, IHostApplicationLifetime appLifetime, IConfiguration configration, IExitService exitService, IWebSocketClient webSocketClient) : base(logger, appLifetime, configration, exitService)
        {
            _webSocketClient = webSocketClient;
        }

        protected override async void OnStarted()
        {
            base.OnStarted();

            IWebInterface webInterace = _webSocketClient as IWebInterface;
            webInterace.Hostname = "localhost";
            webInterace.PortNumber = 8000;

            await _webSocketClient.ConnectAsync();

            // 統一した要求の形式を設けて、そこに要求したい

            await _webSocketClient.SendJsonAsync(new JsonRpcRequest() { Method = "api.v1.cpumodes.localhost.get" });

            // 戻っては来ているが、同期して受け取る処理をまだ書いていない

            Console.WriteLine("Press any key");
            Console.ReadLine();

            (webInterace as IDisposable).Dispose();

            _exitService.Requset(0);
        }
    }
}
