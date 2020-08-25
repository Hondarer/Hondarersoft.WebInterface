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

        public WebSocketServerImpl(ILogger<WebSocketServerImpl> logger, IHostApplicationLifetime appLifetime, IConfiguration configration, ICommonApiManager commonApiManager) : base(logger, appLifetime, configration)
        {
            _commonApiManager = commonApiManager;
        }

        protected override void OnStarted()
        {
            base.OnStarted();

            _commonApiManager.RegistInterface(new WebSocketService())
                .RegistController("Hondarersoft.WebInterface.Sample", "Hondarersoft.WebInterface.Sample.Controllers.CpuModesController") // TODO: 定義ファイルから設定する
                .Start();
        }
    }
}
