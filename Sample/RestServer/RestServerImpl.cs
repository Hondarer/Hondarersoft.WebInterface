using Hondarersoft.Hosting;
using Hondarersoft.WebInterface;
using Hondarersoft.WebInterface.Swagger;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Reflection;
using System.Threading.Tasks;

namespace RestServer
{
    public class RestServerImpl : LifetimeEventsHostedService
    {
        private readonly IHttpService _httpService = null;
        private readonly ISwaggerServerService _swaggerService = null;
        private readonly ICommonApiService _commonApiService = null;

        public RestServerImpl(ILogger<RestServerImpl> logger, IHostApplicationLifetime appLifetime, IConfiguration configuration, IExitService exitService, IHttpService httpService, ISwaggerServerService swaggerService, ICommonApiService commonApiService) : base(logger, appLifetime, configuration, exitService)
        {
            _httpService = httpService;
            _swaggerService = swaggerService;
            _commonApiService = commonApiService;
        }

        protected override async Task OnStartedAsync()
        {
            await base.OnStartedAsync();

            await _commonApiService.RegistInterface(_httpService.LoadConfiguration(_configuration))
                .RegistController(_configuration)
                .StartAsync();

            await _swaggerService.LoadConfiguration(_configuration.GetSection("SwaggerService"))
                .SetSwaggerYamlResolver(() =>
                {
                    var myAssembly = typeof(RestServerImpl).GetTypeInfo().Assembly;
                    return myAssembly.GetManifestResourceStream("RestServer.Swagger.RestServer.yaml");
                })
                .StartAsync();
        }
    }
}