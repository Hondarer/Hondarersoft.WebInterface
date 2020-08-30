using Hondarersoft.Hosting;
using Hondarersoft.WebInterface;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace WebApiServer
{
    public class WebApiServerImpl : LifetimeEventsHostedService
    {
        private readonly IWebApiService _webApiService = null;
        private readonly ICommonApiManager _commonApiManager = null;

        public WebApiServerImpl(ILogger<WebApiServerImpl> logger, IHostApplicationLifetime appLifetime, IConfiguration configration, IExitService exitService, IWebApiService webApiService, ICommonApiManager commonApiManager) : base(logger, appLifetime, configration, exitService)
        {
            _webApiService = webApiService;
            _commonApiManager = commonApiManager;
        }

        protected override async void OnStarted()
        {
            base.OnStarted();

            _webApiService.AllowCORS = true;

            IWebInterface webInterace = _webApiService as IWebInterface;
            webInterace.Hostname = "localhost"; // Hostname を既定の "+" で実行する場合、管理者権限が必要
            webInterace.PortNumber = 8001;
            webInterace.BasePath = "api/v1";

            await _commonApiManager.RegistInterface(webInterace)
                .RegistController("Hondarersoft.WebInterface.Sample", "Hondarersoft.WebInterface.Sample.Controllers.CpuModesController") // TODO: 定義ファイルから設定する
                .StartAsync();
        }
    }
}