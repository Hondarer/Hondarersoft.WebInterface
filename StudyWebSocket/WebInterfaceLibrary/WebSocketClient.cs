using System;
using System.Collections.Generic;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace WebInterfaceLibrary
{
    public class WebSocketClient : WebSocketBase
    {
        protected ClientWebSocket websocket = null;

        public async Task ConnectAsync()
        {
            if ((websocket != null) && ((websocket.State == WebSocketState.Connecting) || (websocket.State == WebSocketState.Open)))
            {
                await websocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "The socket was closed via the ConnectAsync method.", CancellationToken.None);
                websocket = null;
            }

            //接続先エンドポイントを指定
            Uri uri = new Uri("ws://localhost:8000/ws/");

            //サーバに対し、接続を開始
            websocket = new ClientWebSocket();
            await websocket.ConnectAsync(uri, CancellationToken.None);

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

        public override async Task SendTextAsync(string message, WebSocket websocket = null)
        {
            if (websocket == null)
            {
                websocket = this.websocket;
            }

            await base.SendTextAsync(message, websocket);
        }

        public async Task SendJsonAsync(object message, JsonSerializerOptions options = null)
        {
            await base.SendJsonAsync(message, this.websocket, options);
        }

        public override async Task SendJsonAsync(object message, WebSocket websocket = null, JsonSerializerOptions options = null)
        {
            if (websocket == null)
            {
                websocket = this.websocket;
            }

            await base.SendJsonAsync(message, websocket, options);
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
