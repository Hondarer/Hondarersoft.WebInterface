using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;

namespace Hondarersoft.WebInterface
{
    public abstract class WebInterface : IWebInterface, IDisposable
    {
        protected readonly ILogger _logger = null;

        public string Hostname { get; set; } = null;

        public int PortNumber { get; set; } = 0;

        public string BasePath { get; set; } = null;

        public bool UseSSL { get; set; } = false;

        public WebInterface(ILogger<WebInterface> logger)
        {
            _logger = logger;
        }

        public IWebInterface LoadConfiguration(IConfiguration configurationRoot)
        {
            WebInterfaceConfigEntry webInterfaceConfig = configurationRoot.Get<WebInterfaceConfigEntry>();

            return LoadConfiguration(webInterfaceConfig);
        }

        protected IWebInterface LoadConfiguration(WebInterfaceConfigEntry webInterfaceConfig)
        {
            if (string.IsNullOrWhiteSpace(webInterfaceConfig.Hostname) != true)
            {
                Hostname = webInterfaceConfig.Hostname;
            }
            if (webInterfaceConfig.PortNumber != 0)
            {
                PortNumber = webInterfaceConfig.PortNumber;
            }
            if (string.IsNullOrWhiteSpace(webInterfaceConfig.BasePath) != true)
            {
                BasePath = webInterfaceConfig.BasePath;
            }
            if (webInterfaceConfig.UseSSL != null)
            {
                UseSSL = (bool)webInterfaceConfig.UseSSL;
            }

            return this;
        }

        #region IDisposable Support

        private bool disposedValue = false; // 重複する呼び出しを検出するため

        protected virtual void OnDispose(bool disposing)
        {
            if (disposing == true)
            {
                // MEMO: マネージ状態を破棄します (マネージ オブジェクト)。
            }

            // MEMO: アンマネージ リソース (アンマネージ オブジェクト) を解放し、下のファイナライザーをオーバーライドします。
            // MEMO: 大きなフィールドを null に設定します。
        }

        private void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                OnDispose(disposing);

                disposedValue = true;
            }
        }

        ~WebInterface()
        {
            // このコードを変更しないでください。クリーンアップ コードを OnDispose(bool disposing) に記述します。
            Dispose(false);
        }

        public void Dispose()
        {
            // このコードを変更しないでください。クリーンアップ コードを OnDispose(bool disposing) に記述します。
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        #endregion
    }
}
