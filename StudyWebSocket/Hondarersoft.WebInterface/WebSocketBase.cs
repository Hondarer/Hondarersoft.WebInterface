using Hondarersoft.Utility;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace Hondarersoft.WebInterface
{
    public abstract class WebSocketBase : WebInterface, IWebSocketBase
    {
        protected readonly Dictionary<string, WebSocket> webSockets = new Dictionary<string, WebSocket>();
        protected readonly Dictionary<WebSocket, string> webSocketIdentities = new Dictionary<WebSocket, string>();

        public IReadOnlyList<string> WebSocketIdentifies
        {
            get
            {
                return webSockets.Keys.ToList();
            }
        }

        public WebSocketBase(ILogger<WebSocketBase> logger) : base(logger)
        {
            // 既定のエンドポイントは /ws とする。
            BasePath = "ws";
        }

        public virtual Task StartAsync()
        {
            return Task.CompletedTask;
        }

        public event EventHandler<WebSocketRecieveTextEventArgs> WebSocketTextRecieved;
        public event EventHandler<WebSocketEventArgs> WebSocketConnected;
        public event EventHandler<WebSocketEventArgs> WebSocketDisconnected;

        public async Task SendTextAsync(string webSocketIdentify, string message)
        {
            byte[] sendbuffer = Encoding.UTF8.GetBytes(message);
            await SendByteArrayAsTextAsync(webSocketIdentify, sendbuffer);
        }

        public async Task SendJsonAsync(string webSocketIdentify, object message, JsonSerializerOptions options = null)
        {
            byte[] sendbuffer = JsonSerializer.SerializeToUtf8Bytes(message, options);
            await SendByteArrayAsTextAsync(webSocketIdentify, sendbuffer);
        }

        protected async Task SendByteArrayAsTextAsync(string webSocketIdentify, byte[] sendbuffer)
        {
            ArraySegment<byte> segment = new ArraySegment<byte>(sendbuffer);

            // TODO: 重要:
            // このタイミングでソケットが閉じていると、HttpListenerExceptionが発生するため
            // 各処理は例外のハンドリングをきちんと行う必要がある。現状棚卸未。

            _logger.LogInformation("Send to {0}: {1}", webSocketIdentify, Encoding.UTF8.GetString(sendbuffer));

            await webSockets[webSocketIdentify].SendAsync(segment, WebSocketMessageType.Text, true, CancellationToken.None);
        }

        /// <summary>
        /// WebSocket接続毎の処理
        /// </summary>
        protected virtual async Task ProcessRecieve(string webSocketIdentify, WebSocket webSocket)
        {
            webSockets.Add(webSocketIdentify, webSocket);
            webSocketIdentities.Add(webSocket, webSocketIdentify);

            _logger.LogInformation("Session Started. webSocketIdentify = {0}.", webSocketIdentify);

            try
            {
                // 接続完了イベントの処理
                await OnConnected(webSocketIdentify, webSocket);

                //情報取得待ちループ
                while (webSocket.State == WebSocketState.Open)
                {
                    byte[] buffer = new byte[1024];

                    //所得情報確保用の配列を準備
                    ArraySegment<byte> segment = new ArraySegment<byte>(buffer);

                    //サーバからのレスポンス情報を取得
                    WebSocketReceiveResult result = await webSocket.ReceiveAsync(segment, CancellationToken.None);

                    //エンドポイントCloseの場合、処理を中断
                    if (result.MessageType == WebSocketMessageType.Close)
                    {
                        await webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Accept", CancellationToken.None);
                        break;
                    }

                    //バイナリの場合は、当処理では扱えないため、処理を中断
                    if (result.MessageType == WebSocketMessageType.Binary)
                    {
                        await webSocket.CloseAsync(WebSocketCloseStatus.InvalidMessageType, "I don't do binary", CancellationToken.None);
                        break;
                    }

                    //メッセージの最後まで取得
                    // TODO: バッファの自動拡張に対応していない
                    int count = result.Count;
                    while (!result.EndOfMessage)
                    {
                        if (count >= buffer.Length)
                        {
                            await webSocket.CloseAsync(WebSocketCloseStatus.InvalidPayloadData, "That's too long", CancellationToken.None);
                            break;
                        }
                        segment = new ArraySegment<byte>(buffer, count, buffer.Length - count);
                        result = await webSocket.ReceiveAsync(segment, CancellationToken.None);

                        count += result.Count;
                    }

                    //メッセージを取得
                    string message = Encoding.UTF8.GetString(buffer, 0, count);

                    _logger.LogInformation("Recieve from {0}: {1}", webSocketIdentify, message);

                    await OnRecieveText(webSocketIdentify, message);
                }
            }
            catch (Exception ex)
            {
                // 例外 クライアントが異常終了
                _logger.LogWarning("Session aborted. webSocketIdentify = {0}, Description = {1}.\r\n{2}", webSocketIdentify, webSocket.CloseStatusDescription, ex.ToString());
            }
            finally
            {
                webSockets.Remove(webSocketIdentities[webSocket]);
                webSocketIdentities.Remove(webSocket);

                _logger.LogInformation("Session Closed. webSocketIdentify = {0}, Description = {1}.", webSocketIdentify, webSocket.CloseStatusDescription);

                // 接続終了イベントの処理
                await OnDisconnected(webSocketIdentify, webSocket);
            }
        }

        protected virtual Task OnRecieveText(string webSocketIdentify, string message)
        {
            if (WebSocketTextRecieved != null)
            {
                WebSocketTextRecieved(this, new WebSocketRecieveTextEventArgs(webSocketIdentify, message));
            }

            return Task.CompletedTask;
        }

        protected virtual Task OnConnected(string webSocketIdentify, WebSocket webSocket)
        {
            if (WebSocketConnected != null)
            {
                WebSocketConnected(this, new WebSocketEventArgs(webSocketIdentify));
            }

            return Task.CompletedTask;
        }

        protected virtual Task OnDisconnected(string webSocketIdentify, WebSocket webSocket)
        {
            webSocket.Dispose();

            if (WebSocketDisconnected != null)
            {
                WebSocketDisconnected(this, new WebSocketEventArgs(webSocketIdentify));
            }

            return Task.CompletedTask;
        }

        #region IDisposable Support

        protected override void OnDispose(bool disposing)
        {
            if (disposing == true)
            {
                // TODO: マネージ状態を破棄します (マネージ オブジェクト)。
                Parallel.ForEach(webSockets.Values, ws =>
                {
                    if (ws.State == WebSocketState.Open)
                    {
                        ws.CloseAsync(WebSocketCloseStatus.NormalClosure, "The socket was closed via the OnDispose method.", CancellationToken.None).NoWaitAndWatchException();
                    }
                });
            }

            // TODO: アンマネージ リソース (アンマネージ オブジェクト) を解放し、下のファイナライザーをオーバーライドします。
            // TODO: 大きなフィールドを null に設定します。

            base.OnDispose(disposing);
        }

        #endregion
    }
}
