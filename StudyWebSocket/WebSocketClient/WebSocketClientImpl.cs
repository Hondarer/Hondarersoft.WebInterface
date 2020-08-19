using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Text;
using WebInterfaceLibrary;
using WebInterfaceLibrary.Schemas;

namespace WebSocketClient
{
    class WebSocketClientImpl : LifetimeEventsHostedService
    {
        public WebSocketClientImpl(ILogger<WebSocketClientImpl> logger, IHostApplicationLifetime appLifetime, IConfiguration configration) : base(logger, appLifetime, configration)
        {
        }

        protected override async void OnStarted()
        {
            //logger.LogInformation("{0} {1} {2}", configration.GetValue<string>("Option1"), configration.GetValue<int>("Option2"), configration.GetValue<Guid>("Option3"));

            base.OnStarted();

            using (WebInterfaceLibrary.WebSocketClient webSocketClient = new WebInterfaceLibrary.WebSocketClient())
            {
                await webSocketClient.ConnectAsync();

                // 統一した要求の形式を設けて、そこに要求したい

                await webSocketClient.SendJsonAsync(new JsonRpcRequest() { Method = "cpumodes.localhost.get" });

                // 戻っては来ているが、同期して受け取る処理をまだ書いていない
            }


            Console.WriteLine("Press any key");
            Console.ReadLine();
            appLifetime.StopApplication();
        }
    }
}
