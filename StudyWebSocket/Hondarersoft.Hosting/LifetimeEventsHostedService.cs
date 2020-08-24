using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Hondarersoft.Hosting
{
    public class LifetimeEventsHostedService : IHostedService
    {
        protected readonly ILogger logger;
        protected readonly IHostApplicationLifetime appLifetime;
        protected readonly IConfiguration configration;

        public LifetimeEventsHostedService(
            ILogger<LifetimeEventsHostedService> logger, IHostApplicationLifetime appLifetime, IConfiguration configration)
        {
            this.logger = logger;
            this.appLifetime = appLifetime;
            this.configration = configration;
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            appLifetime.ApplicationStarted.Register(OnStarted);
            appLifetime.ApplicationStopping.Register(OnStopping);
            appLifetime.ApplicationStopped.Register(OnStopped);

            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }

        protected virtual void OnStarted()
        {
            logger.LogInformation("OnStarted has been called.");

            // Perform post-startup activities here
        }

        protected virtual void OnStopping()
        {
            logger.LogInformation("OnStopping has been called.");

            // Perform on-stopping activities here
        }

        protected virtual void OnStopped()
        {
            logger.LogInformation("OnStopped has been called.");

            // Perform post-stopped activities here
        }
    }
}
