using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;
using WebSocketLibrary.Schemas;

namespace WebSocketLibrary
{
    public class WebInterfaceBase : IDisposable
    {
        protected static readonly Dictionary<CommonApiArgs.Errors, int> ErrorsToCode = new Dictionary<CommonApiArgs.Errors, int>()
        {
            {CommonApiArgs.Errors.ParseError,-32700},
            {CommonApiArgs.Errors.InvalidRequest,-32600},
            {CommonApiArgs.Errors.MethodNotFound,-32601}, // same
            {CommonApiArgs.Errors.MethodNotAvailable,-32601}, // same
            {CommonApiArgs.Errors.InvalidParams,-32602},
            {CommonApiArgs.Errors.InternalError,-32603},
        };

        public virtual void OnRequest(CommonApiArgs commonApiArgs)
        {
            // TODO: この部分は基本の通信処理と分けて考えるべき

            // ★★★ テスト ★★★
            if (commonApiArgs.Path.Equals("/cpumodes") && commonApiArgs.Method== CommonApiArgs.Methods.GET)
            {
                commonApiArgs.ResponseBody = new CpuModes() { new CpuMode() { Hostname = "localhoost" }, new CpuMode() { Hostname = "hostname2" } };
            }
            else if (commonApiArgs.Path.Equals("/cpumodes/localhost") && commonApiArgs.Method == CommonApiArgs.Methods.GET)
            {
                commonApiArgs.ResponseBody = new CpuMode() { Hostname = "localhoost" };
            }
            else
            {
                commonApiArgs.SetError(CommonApiArgs.Errors.MethodNotFound);
            }
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
