// [C#]System.Net.WebSocketsを試す。その２。サーバー編。
// http://kimux.net/?p=956

using System;
using System.Collections.Generic;
using System.Net;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace WebSocketLibrary
{
    public class WebSocketServer : IDisposable
    {
        /// <summary>
        /// クライアントのWebSocketインスタンスを格納
        /// </summary>
        private readonly List<WebSocket> clients = new List<WebSocket>();

        /// <summary>
        /// WebSocketサーバースタート
        /// </summary>
        public async void Run()
        {
            /// httpListenerで待ち受け
            HttpListener httpListener = new HttpListener();
            httpListener.Prefixes.Add("http://localhost:8000/ws/");
            httpListener.Start();

            while (true)
            {
                /// 接続待機
                HttpListenerContext listenerContext = await httpListener.GetContextAsync();
                if (listenerContext.Request.IsWebSocketRequest)
                {
                    /// httpのハンドシェイクがWebSocketならWebSocket接続開始
                    ProcessRequest(listenerContext);
                }
                else
                {
                    /// httpレスポンスを返す
                    listenerContext.Response.StatusCode = 400;
                    listenerContext.Response.Close();
                }
            }
        }

        /// <summary>
        /// WebSocket接続毎の処理
        /// </summary>
        /// <param name="listenerContext"></param>
        private async void ProcessRequest(HttpListenerContext listenerContext)
        {
            Console.WriteLine("{0}:New Session:{1}", DateTime.Now.ToString(), listenerContext.Request.RemoteEndPoint.Address.ToString());

            /// WebSocketの接続完了を待機してWebSocketオブジェクトを取得する
            WebSocket ws = (await listenerContext.AcceptWebSocketAsync(subProtocol: null)).WebSocket;

            /// 新規クライアントを追加
            clients.Add(ws);

            /// WebSocketの送受信ループ
            while (ws.State == WebSocketState.Open)
            {
                try
                {
                    //１０回のレスポンスを返却
                    for (int i = 0; i != 10; ++i)
                    {
                        //1回のレスポンスごとに2秒のウエイトを設定
                        await Task.Delay(2000);

                        //レスポンスのテストメッセージとして、現在時刻の文字列を取得
                        string time = DateTime.Now.ToLongTimeString();

                        //文字列をByte型に変換
                        byte[] buffer = Encoding.UTF8.GetBytes(time);
                        ArraySegment<byte> segment = new ArraySegment<byte>(buffer);

                        //クライアント側に文字列を送信
                        await ws.SendAsync(segment, WebSocketMessageType.Text,
                          true, CancellationToken.None);
                    }

                    //接続を閉じる
                    await ws.CloseAsync(WebSocketCloseStatus.NormalClosure,
                      "Done", CancellationToken.None);
                }
                catch
                {
                    /// 例外 クライアントが異常終了しやがった
                    Console.WriteLine("{0}:Session Abort:{1}", DateTime.Now.ToString(), listenerContext.Request.RemoteEndPoint.Address.ToString());
                    break;
                }
            }

            Console.WriteLine("{0}:Session End:{1}", DateTime.Now.ToString(), listenerContext.Request.RemoteEndPoint.Address.ToString());

            /// クライアントを除外する
            clients.Remove(ws);
            ws.Dispose();
        }

        #region IDisposable Support

        private bool disposedValue = false; // 重複する呼び出しを検出するには

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    // TODO: マネージ状態を破棄します (マネージ オブジェクト)。
                    Parallel.ForEach(clients, ws =>
                    {
                        if (ws.State == WebSocketState.Open)
                        {
                            ws.CloseAsync(WebSocketCloseStatus.NormalClosure, "", System.Threading.CancellationToken.None);
                        }
                    });

                    Console.WriteLine("{0}:Disposed", DateTime.Now.ToString());
                }

                // TODO: アンマネージ リソース (アンマネージ オブジェクト) を解放し、下のファイナライザーをオーバーライドします。
                // TODO: 大きなフィールドを null に設定します。

                disposedValue = true;
            }
        }

        // TODO: 上の Dispose(bool disposing) にアンマネージ リソースを解放するコードが含まれる場合にのみ、ファイナライザーをオーバーライドします。
        // ~WebSocketServer()
        // {
        //   // このコードを変更しないでください。クリーンアップ コードを上の Dispose(bool disposing) に記述します。
        //   Dispose(false);
        // }

        // このコードは、破棄可能なパターンを正しく実装できるように追加されました。
        public void Dispose()
        {
            // このコードを変更しないでください。クリーンアップ コードを上の Dispose(bool disposing) に記述します。
            Dispose(true);
            // TODO: 上のファイナライザーがオーバーライドされる場合は、次の行のコメントを解除してください。
            // GC.SuppressFinalize(this);
        }

        #endregion
    }
}
