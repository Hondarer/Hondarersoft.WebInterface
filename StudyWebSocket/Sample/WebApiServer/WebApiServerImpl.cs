using Hondarersoft.Hosting;
using Hondarersoft.WebInterface;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace WebApiServer
{
    public class WebApiServerImpl : LifetimeEventsHostedService
    {
        private readonly ICommonApiManager _commonApiManager = null;

        public WebApiServerImpl(ILogger<WebApiServerImpl> logger, IHostApplicationLifetime appLifetime, IConfiguration configration, ICommonApiManager commonApiManager, IExitService exitService) : base(logger, appLifetime, configration, exitService)
        {
            _commonApiManager = commonApiManager;
        }

        protected override void OnStarted()
        {
            base.OnStarted();

            _commonApiManager.RegistInterface(new WebApiService() { AllowCORS = true, PortNumber = 8001, BasePath = "api/v1", Hostname = "localhost" }) // Hostname を既定の "+" で実行する場合、管理者権限が必要
                .RegistController("Hondarersoft.WebInterface.Sample", "Hondarersoft.WebInterface.Sample.Controllers.CpuModesController") // TODO: 定義ファイルから設定する
                .Start();
        }
    }
}