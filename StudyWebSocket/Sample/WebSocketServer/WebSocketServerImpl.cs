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
        private readonly ICommonApiService _commonApiService = null;

        public WebSocketServerImpl(ILogger<WebSocketServerImpl> logger, IHostApplicationLifetime appLifetime, IConfiguration configuration, IExitService exitService, IWebSocketService webSocketService, ICommonApiService commonApiService) : base(logger, appLifetime, configuration, exitService)
        {
            _webSocketService = webSocketService;
            _commonApiService = commonApiService;
        }

        protected override async Task OnStartedAsync()
        {
            await base.OnStartedAsync();

            _webSocketService.LoadConfiguration(_configuration);

            await _commonApiService.RegistInterface(_webSocketService)
                .RegistController(_configuration)
                .StartAsync();
        }
    }
}
