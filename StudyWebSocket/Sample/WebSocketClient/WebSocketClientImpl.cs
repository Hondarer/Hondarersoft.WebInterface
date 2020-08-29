using Hondarersoft.Hosting;
using Hondarersoft.WebInterface.Schemas;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;

namespace WebSocketClient
{
    class WebSocketClientImpl : LifetimeEventsHostedService
    {
        public WebSocketClientImpl(ILogger<WebSocketClientImpl> logger, IHostApplicationLifetime appLifetime, IConfiguration configration, IExitService exitService) : base(logger, appLifetime, configration, exitService)
        {
        }

        protected override async void OnStarted()
        {
            base.OnStarted();

            using (Hondarersoft.WebInterface.WebSocketClient webSocketClient = new Hondarersoft.WebInterface.WebSocketClient() { PortNumber = 8000, Hostname="localhost" })
            {
                await webSocketClient.ConnectAsync();

                // 統一した要求の形式を設けて、そこに要求したい

                await webSocketClient.SendJsonAsync(new JsonRpcRequest() { Method = "api.v1.cpumodes.localhost.get" });

                // 戻っては来ているが、同期して受け取る処理をまだ書いていない
            }

            Console.WriteLine("Press any key");
            Console.ReadLine();

            _exitService.Requset(0);
        }
    }
}
