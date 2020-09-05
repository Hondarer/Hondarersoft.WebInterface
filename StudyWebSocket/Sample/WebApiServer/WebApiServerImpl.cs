using Hondarersoft.Hosting;
using Hondarersoft.WebInterface;
using Hondarersoft.WebInterface.Swagger;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Reflection;
using System.Threading.Tasks;

namespace WebApiServer
{
    public class WebApiServerImpl : LifetimeEventsHostedService
    {
        private readonly IWebApiService _webApiService = null;
        private readonly ISwaggerServerService _swaggerService = null;
        private readonly ICommonApiService _commonApiService = null;

        public WebApiServerImpl(ILogger<WebApiServerImpl> logger, IHostApplicationLifetime appLifetime, IConfiguration configuration, IExitService exitService, IWebApiService webApiService, ISwaggerServerService swaggerService, ICommonApiService commonApiService) : base(logger, appLifetime, configuration, exitService)
        {
            _webApiService = webApiService;
            _swaggerService = swaggerService;
            _commonApiService = commonApiService;
        }

        protected override async Task OnStartedAsync()
        {
            await base.OnStartedAsync();

            _webApiService.AllowCORS = true;

            _webApiService.LoadConfiguration(_configuration);
            await _commonApiService.RegistInterface(_webApiService)
                .RegistController(_configuration)
                .StartAsync();

            _swaggerService.LoadConfiguration(_configuration.GetSection("SwaggerService"));
            await _swaggerService
                .SetSwaggerYamlResolver(() =>
                {
                    var myAssembly = typeof(WebApiServerImpl).GetTypeInfo().Assembly;
                    return myAssembly.GetManifestResourceStream("WebApiServer.Swagger.WebApiServer.yaml");
                })
                .StartAsync();
        }
    }
}