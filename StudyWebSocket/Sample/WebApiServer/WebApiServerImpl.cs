using Hondarersoft.Hosting;
using Hondarersoft.WebInterface;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;

namespace WebApiServer
{
    public class WebApiServerImpl : LifetimeEventsHostedService
    {
        private readonly IWebApiService _webApiService = null;
        private readonly ICommonApiService _commonApiService = null;

        public WebApiServerImpl(ILogger<WebApiServerImpl> logger, IHostApplicationLifetime appLifetime, IConfiguration configuration, IExitService exitService, IWebApiService webApiService, ICommonApiService commonApiService) : base(logger, appLifetime, configuration, exitService)
        {
            _webApiService = webApiService;
            _commonApiService = commonApiService;
        }

        protected override async Task OnStartedAsync()
        {
            await base.OnStartedAsync();

            _webApiService.AllowCORS = true;

            IWebInterface webInterace = _webApiService as IWebInterface;
            webInterace.Hostname = "localhost"; // Hostname を既定の "+" で実行する場合、管理者権限が必要
            webInterace.PortNumber = 8001;
            webInterace.BasePath = "api/v1";

            await _commonApiService.RegistInterface(webInterace)
                .RegistController(_configuration)
                .StartAsync();
        }
    }
}