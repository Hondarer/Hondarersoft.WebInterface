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
        protected readonly IConfiguration _configration = null;
        protected readonly IExitService _exitService = null;

        /// <summary>
        /// 明示的に指定されない異常終了の終了コードを取得または設定します。
        /// </summary>
        public int ErrorExitCode { get; set; } = 1;

        public LifetimeEventsHostedService(
            ILogger<LifetimeEventsHostedService> logger, IHostApplicationLifetime appLifetime, IConfiguration configration, IExitService exitService)
        {
            _logger = logger;
            _appLifetime = appLifetime;
            _configration = configration;
            _exitService = exitService;

            TaskScheduler.UnobservedTaskException += OnUnobservedTaskException;
        }

        private void OnUnobservedTaskException(object sender, UnobservedTaskExceptionEventArgs e)
        {
            // Task.Run によりハンドルされない例外があった場合、それが GC された際に発生する。
            // GC のタイミングなので、この処理が確実に動作するかどうかは保証できない。
            // 基本的には各処理で正しく try - catch を行うこと。
            _logger.LogCritical("UnobservedTaskException has occurred.\r\n{0}", e.Exception.ToString());

            // 本来の Generic Host の考えであれば、他のサービスを巻き込んで
            // Host 全体を止めるかどうかは設計の問題であり、直ちに決められないが、
            // 本実装では安全のため停止させることとしている。
            _exitService.Requset(ErrorExitCode);
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            OnStarting();

            _appLifetime.ApplicationStarted.Register(Started);
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
            // このメソッド内の例外は、
            // Generic Host の外側にスローされる。
            // Started へと進めるべきでないので、ここで catch しない。

            _logger.LogInformation("OnStarting has been called.");

            // Perform on-startup activities here
        }

        private void Started()
        {
            try
            {
                // このメソッド内で例外が発生しても、プログラムは異常終了しないので、
                // ここでキャッチして終了させる。

                // 本来の Generic Host の考えであれば、他のサービスを巻き込んで
                // Host 全体を止めるかどうかは設計の問題であり、直ちに決められないが、
                // 本実装では安全のため停止させることとしている。
                OnStarted();
            }
            catch (Exception ex)
            {
                _logger.LogCritical("An error occurred starting the application.\r\n{0}", ex);

                _exitService.Requset(ErrorExitCode);
            }
        }

        protected virtual void OnStarted()
        {
            _logger.LogInformation("OnStarted has been called.");

            // Perform post-startup activities here
        }

        protected virtual void OnStopping()
        {
            // このメソッド内の例外は、
            // ログされた上でアプリケーションが終了する。

            _logger.LogInformation("OnStopping has been called.");

            // Perform on-stopping activities here
        }

        protected virtual void OnStopped()
        {
            // このメソッド内の例外は、
            // ログされた上でアプリケーションが終了する。

            _logger.LogInformation("OnStopped has been called.");

            // Perform post-stopped activities here
        }
    }
}
