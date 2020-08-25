using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Threading;
using System.Threading.Tasks;

namespace Hondarersoft.Hosting
{
    public class LifetimeEventsHostedService : IHostedService
    {
        protected readonly ILogger _logger = null;
        protected readonly IHostApplicationLifetime _appLifetime = null;
        protected readonly IConfiguration _configration = null;

        public LifetimeEventsHostedService(
            ILogger<LifetimeEventsHostedService> logger, IHostApplicationLifetime appLifetime, IConfiguration configration)
        {
            _logger = logger;
            _appLifetime = appLifetime;
            _configration = configration;
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            OnStarting();

            _appLifetime.ApplicationStarted.Register(OnStarted);
            _appLifetime.ApplicationStopping.Register(OnStopping);
            _appLifetime.ApplicationStopped.Register(OnStopped);

            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }

        protected virtual void OnStarting()
        {
            _logger.LogInformation("OnStarting has been called.");

            // Perform on-startup activities here
        }

        protected virtual void OnStarted()
        {
            _logger.LogInformation("OnStarted has been called.");

            // Perform post-startup activities here
        }

        protected virtual void OnStopping()
        {
            _logger.LogInformation("OnStopping has been called.");

            // Perform on-stopping activities here
        }

        protected virtual void OnStopped()
        {
            _logger.LogInformation("OnStopped has been called.");

            // Perform post-stopped activities here
        }
    }
}
