using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Text;
using Hondarersoft.WebInterface;
using Hondarersoft.Hosting;

namespace WebApiServer
{
    public class WebApiServerImpl : LifetimeEventsHostedService
    {
        ICommonApiManager commonApiManager;

        public WebApiServerImpl(ILogger<WebApiServerImpl> logger, IHostApplicationLifetime appLifetime, IConfiguration configration, ICommonApiManager commonApiManager) : base(logger, appLifetime, configration)
        {
            this.commonApiManager = commonApiManager;
        }

        protected override void OnStarted()
        {
            //logger.LogInformation("{0} {1} {2}", configration.GetValue<string>("Option1"), configration.GetValue<int>("Option2"), configration.GetValue<Guid>("Option3"));

            base.OnStarted();

            commonApiManager.Regist(new WebApiService() { AllowCORS = true }).Start(); // TODO: DIに対応する
        }
    }
}