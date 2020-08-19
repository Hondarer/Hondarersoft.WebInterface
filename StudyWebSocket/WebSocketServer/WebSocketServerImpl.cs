using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Text;
using WebInterfaceLibrary;

namespace WebSocketServer
{
    public class WebSocketServerImpl : LifetimeEventsHostedService
    {
        CommonApiManager commonApiManager; // TODO: DI化

        public WebSocketServerImpl(ILogger<WebSocketServerImpl> logger, IHostApplicationLifetime appLifetime, IConfiguration configration) : base(logger, appLifetime, configration)
        {
        }

        protected override void OnStarted()
        {
            //logger.LogInformation("{0} {1} {2}", configration.GetValue<string>("Option1"), configration.GetValue<int>("Option2"), configration.GetValue<Guid>("Option3"));

            base.OnStarted();

            commonApiManager = new CommonApiManager().Regist(new WebSocketService()).Start(); // TODO: DIに対応する

            //Console.WriteLine("Press any key");
            //Console.ReadLine();
            //appLifetime.StopApplication();
        }
    }
}
