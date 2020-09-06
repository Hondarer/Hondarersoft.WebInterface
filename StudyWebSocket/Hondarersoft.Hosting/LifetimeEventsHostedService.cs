using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Hondarersoft.Hosting
{
    public class LifetimeEventsHostedService : IHostedService
    {
        protected readonly ILogger _logger = null;
        private readonly IHostApplicationLifetime _appLifetime = null;
        protected readonly IConfiguration _configuration = null;
        protected readonly IExitService _exitService = null;

        /// <summary>
        /// 明示的に指定されない異常終了の終了コードを取得または設定します。
        /// </summary>
        public int ErrorExitCode { get; set; } = 1;

        public LifetimeEventsHostedService(
            ILogger<LifetimeEventsHostedService> logger, IHostApplicationLifetime appLifetime, IConfiguration configuration, IExitService exitService)
        {
            _logger = logger;
            _appLifetime = appLifetime;
            _configuration = configuration;
            _exitService = exitService;

            // Task.NoWait() による戻り値を管理しない Task の例外を補足する。
            Utility.TaskExtensions.NoWaitTaskException += NoWaitTaskException;

            // TaskExtensions を利用しなかったケースでの最終救済策。
            // Task.Run や async void によりハンドルされない例外があった場合、それが GC された際に発生する。
            // GC のタイミングなので、この処理が確実に動作するかどうかは保証できない。
            // 基本的には各処理で正しく try - catch を行い、Task.NoWait() を用いて
            // 処理を行うこと。
            TaskScheduler.UnobservedTaskException += OnUnobservedTaskException;
        }

        private void NoWaitTaskException(object sender, Utility.NoWaitTaskExceptionEventArgs e)
        {
            _logger.Log(e.LogLevel, "NoWaitTaskExceptionEventArgs has occurred.\r\n{0}", e.Exception.ToString());

            if (e.LogLevel == LogLevel.Critical)
            {
                // 致命的とマークされた補足されない例外が発生したときは、ホストを停止させる。
                _exitService.Request(ErrorExitCode);
            }
        }

        protected virtual void OnUnobservedTaskException(object sender, UnobservedTaskExceptionEventArgs e)
        {
            _logger.LogCritical("UnobservedTaskException has occurred.\r\n{0}", e.Exception.ToString());

            // 本来の Generic Host の考えであれば、他のサービスを巻き込んで
            // Host 全体を止めるかどうかは設計の問題であり、直ちに決められないが、
            // 本実装では安全のため停止させることとしている。
            _exitService.Request(ErrorExitCode);
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            Starting();

            _appLifetime.ApplicationStarted.Register(Started);
            _appLifetime.ApplicationStopping.Register(Stopping);
            _appLifetime.ApplicationStopped.Register(Stopped);

            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }

        private void Starting()
        {
            try
            {
                _logger.LogInformation("OnStarting has been called.");
                OnStartingAsync().Wait();
            }
            catch (Exception ex)
            {
                _logger.LogCritical("An error occurred starting the application.\r\n{0}", ex);

                _exitService.Request(ErrorExitCode);
            }
        }

        protected virtual Task OnStartingAsync()
        {
            return Task.CompletedTask;
        }

        private void Started()
        {
            try
            {
                _logger.LogInformation("OnStarted has been called.");
                OnStartedAsync().Wait();
            }
            catch (Exception ex)
            {
                _logger.LogCritical("An error occurred starting the application.\r\n{0}", ex);

                _exitService.Request(ErrorExitCode);
            }
        }

        protected virtual Task OnStartedAsync()
        {
            return Task.CompletedTask;
        }

        private void Stopping()
        {
            try
            {
                _logger.LogInformation("OnStopping has been called.");
                OnStoppingAsync().Wait();
            }
            catch (Exception ex)
            {
                _logger.LogCritical("An error occurred stopping the application.\r\n{0}", ex);

                _exitService.Request(ErrorExitCode);
            }
        }

        protected virtual Task OnStoppingAsync()
        {
            return Task.CompletedTask;
        }

        private void Stopped()
        {
            try
            {
                _logger.LogInformation("OnStopped has been called.");
                OnStoppedAsync().Wait();
            }
            catch (Exception ex)
            {
                _logger.LogCritical("An error occurred stopping the application.\r\n{0}", ex);

                _exitService.Request(ErrorExitCode);
            }
        }

        protected virtual Task OnStoppedAsync()
        {
            return Task.CompletedTask;
        }
    }
}
