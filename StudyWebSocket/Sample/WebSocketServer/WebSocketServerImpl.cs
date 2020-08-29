using Hondarersoft.Hosting;
using Hondarersoft.WebInterface;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace WebSocketServer
{
    public class WebSocketServerImpl : LifetimeEventsHostedService
    {
        private readonly ICommonApiManager _commonApiManager;

        public WebSocketServerImpl(ILogger<WebSocketServerImpl> logger, IHostApplicationLifetime appLifetime, IConfiguration configration, ICommonApiManager commonApiManager, IExitService exitService) : base(logger, appLifetime, configration, exitService)
        {
            _commonApiManager = commonApiManager;
        }

        protected override void OnStarted()
        {
            base.OnStarted();

            _commonApiManager.RegistInterface(new WebSocketService() { PortNumber = 8000, Hostname="localhost" }) // Hostname を既定の "+" で実行する場合、管理者権限が必要
                .RegistController("Hondarersoft.WebInterface.Sample", "Hondarersoft.WebInterface.Sample.Controllers.CpuModesController") // TODO: 定義ファイルから設定する
                .Start();
        }
    }
}
