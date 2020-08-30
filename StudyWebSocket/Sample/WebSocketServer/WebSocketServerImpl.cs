using Hondarersoft.Hosting;
using Hondarersoft.WebInterface;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;

namespace WebSocketServer
{
    public class WebSocketServerImpl : LifetimeEventsHostedService
    {
        private readonly IWebSocketService _webSocketService = null;
        private readonly ICommonApiManager _commonApiManager = null;

        public WebSocketServerImpl(ILogger<WebSocketServerImpl> logger, IHostApplicationLifetime appLifetime, IConfiguration configration, IExitService exitService, IWebSocketService webSocketService, ICommonApiManager commonApiManager) : base(logger, appLifetime, configration, exitService)
        {
            _webSocketService = webSocketService;
            _commonApiManager = commonApiManager;
        }

        protected override async Task OnStartedAsync()
        {
            await base.OnStartedAsync();

            IWebInterface webInterace = _webSocketService as IWebInterface;
            webInterace.Hostname = "localhost"; // Hostname を既定の "+" で実行する場合、管理者権限が必要
            webInterace.PortNumber = 8000;

            await _commonApiManager.RegistInterface(webInterace)
                .RegistController("Hondarersoft.WebInterface.Sample", "Hondarersoft.WebInterface.Sample.Controllers.CpuModesController") // TODO: 定義ファイルから設定する
                .StartAsync();
        }
    }
}
