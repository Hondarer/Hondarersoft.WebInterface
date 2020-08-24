using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Text;
using Hondarersoft.WebInterface;
using Hondarersoft.Hosting;

namespace WebSocketServer
{
    public class WebSocketServerImpl : LifetimeEventsHostedService
    {
        ICommonApiManager commonApiManager;

        public WebSocketServerImpl(ILogger<WebSocketServerImpl> logger, IHostApplicationLifetime appLifetime, IConfiguration configration, ICommonApiManager commonApiManager) : base(logger, appLifetime, configration)
        {
            this.commonApiManager = commonApiManager;
        }

        protected override void OnStarted()
        {
            //logger.LogInformation("{0} {1} {2}", configration.GetValue<string>("Option1"), configration.GetValue<int>("Option2"), configration.GetValue<Guid>("Option3"));

            base.OnStarted();

            commonApiManager.Regist(new WebSocketService()).Start();

            //Console.WriteLine("Press any key");
            //Console.ReadLine();
            //appLifetime.StopApplication();
        }
    }
}
