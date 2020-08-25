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

        public WebApiServerImpl(ILogger<WebApiServerImpl> logger, IHostApplicationLifetime appLifetime, IConfiguration configration, ICommonApiManager commonApiManager) : base(logger, appLifetime, configration)
        {
            _commonApiManager = commonApiManager;
        }

        protected override void OnStarted()
        {
            base.OnStarted();

            _commonApiManager.RegistInterface(new WebApiService() { AllowCORS = true })
                .RegistController("Hondarersoft.WebInterface.Sample", "Hondarersoft.WebInterface.Sample.Controllers.CpuModesController") // TODO: 定義ファイルから設定する
                .Start();
        }
    }
}