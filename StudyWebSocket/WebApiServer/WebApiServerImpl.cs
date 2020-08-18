using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Text;
using WebInterfaceLibrary;

namespace WebApiServer
{
    public class WebApiServerImpl : LifetimeEventsHostedService
    {
        private readonly IConfiguration configration; // TODO: ベースクラスにもっていく

        CommonApiManager commonApiManager;

        public WebApiServerImpl(ILogger<WebApiServerImpl> logger, IHostApplicationLifetime appLifetime, IConfiguration configration) : base(logger, appLifetime)
        {
            this.configration = configration; // TODO: ベースクラスにもっていく
        }

        protected override void OnStarted()
        {
            //logger.LogInformation("{0} {1} {2}", configration.GetValue<string>("Option1"), configration.GetValue<int>("Option2"), configration.GetValue<Guid>("Option3"));

            base.OnStarted();

            commonApiManager = new CommonApiManager().Regist(new WebApiService() { AllowCORS = true }).Start(); // TODO: DIに対応する
        }
    }
}