using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;

namespace Hondarersoft.Hosting
{
    public class ExitService : IExitService
    {
        protected readonly ILogger _logger = null;
        protected readonly IHostApplicationLifetime _appLifetime = null;

        public int? ExitCode { get; private set; } = null;

        public bool IsExiting
        {
            get
            {
                return ExitCode != null;
            }
        }

        public ExitService(ILogger<ExitService> logger, IHostApplicationLifetime appLifetime)
        {
            _logger = logger;
            _appLifetime = appLifetime;
        }

        public bool Requset(int exitCode)
        {
            // 複数スレッドからの呼び出しに対応できるよう、
            // ダブル チェック ロッキング パターンを取る。

            if (ExitCode != null)
            {
                _logger.LogWarning("Application stop request was already requested.");
                return false;
            }

            lock (this)
            {
                if (ExitCode != null)
                {
                    _logger.LogWarning("Application stop request was already requested.");
                    return false;
                }

                ExitCode = exitCode;
                Environment.ExitCode = exitCode;

                _logger.LogInformation("Application stop request was accepted. exitCode = {0}.", exitCode);

                _appLifetime.StopApplication();

                return true;
            }
        }
    }
}
