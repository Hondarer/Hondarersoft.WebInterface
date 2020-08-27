using System;
using System.Collections.Generic;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace Hondarersoft.WebInterface
{
    public class WebSocketClient : WebSocketBase
    {
        protected ClientWebSocket websocket = null;

        public override async void Start()
        {
            base.Start();

            await ConnectAsync();
        }

        public async Task ConnectAsync()
        {
            if ((string.IsNullOrEmpty(Hostname) == true) ||
                (PortNumber == 0) ||
                (string.IsNullOrEmpty(BasePath) == true))
            {
                throw new Exception("invalid endpoint parameter");
            }

            if ((websocket != null) && ((websocket.State == WebSocketState.Connecting) || (websocket.State == WebSocketState.Open)))
            {
                await websocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "The socket was closed via the ConnectAsync method.", CancellationToken.None);
                websocket = null;
            }

            //接続先エンドポイントを指定

            string ssl = string.Empty;
            if (UseSSL == true)
            {
                ssl = "s";
            }
            Uri uri = new Uri($"ws{ssl}://{Hostname}:{PortNumber}/{BasePath}/");

            //サーバに対し、接続を開始

            int retry = 0;

            while (true) 
            {
                try
                {
                    websocket = new ClientWebSocket();
                    await websocket.ConnectAsync(uri, CancellationToken.None);
                    break;
                }
                catch (WebSocketException)
                {
                    websocket.Dispose();
                    websocket = null;

                    retry++;

                    Console.WriteLine($"Unable to connect {retry}/3 time(s). Retry after 3000 milliseconds."); // TODO: ILogger & Const

                    if (retry >= 3) // TODO: メソッド引数に
                    {
                        throw;
                    }

                    Thread.Sleep(3000);
                }
            }

            ProcessRecieve(websocket);
        }

        public async Task CloseAsync()
        {
            if ((websocket != null) && ((websocket.State == WebSocketState.Connecting) || (websocket.State == WebSocketState.Open)))
            {
                await websocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "The socket was closed via the CloseAsync method.", CancellationToken.None);
                websocket = null;
            }
        }

        public new async Task SendTextAsync(string message, WebSocket websocket = null)
        {
            if (websocket == null)
            {
                websocket = this.websocket;
            }

            await WebSocketBase.SendTextAsync(message, websocket);
        }

        //public async Task SendJsonAsync(object message, JsonSerializerOptions options = null)
        //{
        //    if (options == null)
        //    {
        //        options = new JsonSerializerOptions
        //        {
        //            IgnoreNullValues = true
        //        };
        //    }

        //    await WebSocketBase.SendJsonAsync(message, this.websocket, options);
        //}

        public new async Task SendJsonAsync(object message, WebSocket websocket = null, JsonSerializerOptions options = null)
        {
            if (options == null)
            {
                options = new JsonSerializerOptions
                {
                    IgnoreNullValues = true
                };
            }

            if (websocket == null)
            {
                websocket = this.websocket;
            }

            await WebSocketBase.SendJsonAsync(message, websocket, options);
        }

        #region IDisposable Support

        protected override void OnDispose(bool disposing)
        {
            if (disposing == true)
            {
                // TODO: マネージ状態を破棄します (マネージ オブジェクト)。
                if ((websocket != null) && ((websocket.State == WebSocketState.Connecting) || (websocket.State == WebSocketState.Open)))
                {
                    websocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "The socket was closed via the OnDispose method.", CancellationToken.None);
                    websocket = null;
                }

                Console.WriteLine("{0}:Disposed", DateTime.Now.ToString());
            }

            // TODO: アンマネージ リソース (アンマネージ オブジェクト) を解放し、下のファイナライザーをオーバーライドします。
            // TODO: 大きなフィールドを null に設定します。

            base.OnDispose(disposing);
        }

        #endregion
    }
}
