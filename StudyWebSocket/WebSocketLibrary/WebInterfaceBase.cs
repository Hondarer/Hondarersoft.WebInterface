using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;
using WebSocketLibrary.Schemas;

namespace WebSocketLibrary
{
    public class WebInterfaceBase : IDisposable
    {
        public virtual void OnRequest(CommonApiArgs commonApiArgs)
        {

            // ★★★ テスト ★★★
            if (commonApiArgs.Path.Equals("/cpumodes") && commonApiArgs.Method== CommonApiArgs.Methods.GET)
            {
                // SerializeToUtf8Bytes を使ったほうが速いが、トレースしたいので当面 string 経由とする。
                commonApiArgs.ResponseBody = JsonSerializer.Serialize(new CpuModes() { new CpuMode() { Hostname = "localhoost" }, new CpuMode() { Hostname = "hostname2" } });
            }
            if (commonApiArgs.Path.Equals("/cpumodes/localhost") && commonApiArgs.Method == CommonApiArgs.Methods.GET)
            {
                commonApiArgs.ResponseBody = JsonSerializer.Serialize(new CpuMode() { Hostname = "localhoost" });
            }

            commonApiArgs.ResponseBody = JsonSerializer.Serialize(new Error() { Message = "No such method" });
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

        ~WebInterfaceBase()
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
