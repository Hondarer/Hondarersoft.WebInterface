using System;
using System.Collections.Generic;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace WebSocketLibrary
{
    public class WebSocketBase : WebInterfaceBase
    {
        public virtual async Task SendTextAsync(string message, WebSocket webSocket)
        {
            byte[] sendbuffer = Encoding.UTF8.GetBytes(message);
            await SendBufferAsync(sendbuffer, webSocket);
        }

        public virtual async Task SendJsonAsync(object message, WebSocket webSocket)
        {
            byte[] sendbuffer = JsonSerializer.SerializeToUtf8Bytes(message);
            await SendBufferAsync(sendbuffer, webSocket);
        }

        protected async Task SendBufferAsync(byte[] sendbuffer, WebSocket webSocket)
        {
            ArraySegment<byte> segment = new ArraySegment<byte>(sendbuffer);
            await webSocket.SendAsync(segment, WebSocketMessageType.Text, true, CancellationToken.None);
        }

        /// <summary>
        /// WebSocket接続毎の処理
        /// </summary>
        /// <param name="listenerContext"></param>
        protected virtual async void ProcessRecieve(WebSocket webSocket)
        {
            await OnConnected(webSocket);

            try
            {
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
                    Console.WriteLine("> " + message);

                    await OnRecieveText(webSocket, message);
                }
            }
            catch (Exception ex)
            {
                /// 例外 クライアントが異常終了しやがった
                Console.WriteLine("{0}:Session Abort:{1} {2}", DateTime.Now.ToString(), webSocket.CloseStatusDescription, ex.ToString());
            }
            finally
            {
                await OnClosed(webSocket);
                Console.WriteLine("{0}:Session End:{1}", DateTime.Now.ToString(), webSocket.CloseStatusDescription);
            }
        }

#pragma warning disable CS1998 // 非同期メソッドは、'await' 演算子がないため、同期的に実行されます
        protected virtual async Task OnRecieveText(WebSocket webSocket, string message)
#pragma warning restore CS1998 // 非同期メソッドは、'await' 演算子がないため、同期的に実行されます
        {
        }

#pragma warning disable CS1998 // 非同期メソッドは、'await' 演算子がないため、同期的に実行されます
        protected virtual async Task OnConnected(WebSocket webSocket)
#pragma warning restore CS1998 // 非同期メソッドは、'await' 演算子がないため、同期的に実行されます
        {
        }

#pragma warning disable CS1998 // 非同期メソッドは、'await' 演算子がないため、同期的に実行されます
        protected virtual async Task OnClosed(WebSocket webSocket)
#pragma warning restore CS1998 // 非同期メソッドは、'await' 演算子がないため、同期的に実行されます
        {
        }
    }
}
