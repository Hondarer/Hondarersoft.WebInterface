using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;

namespace Hondarersoft.WebInterface
{
    public class WebInterface : IWebInterface, IDisposable
    {
        public string Hostname { get; set; } = null;

        public int PortNumber { get; set; } = 0;

        public string BasePath { get; set; } = null;

        public bool UseSSL { get; set; } = false;

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
