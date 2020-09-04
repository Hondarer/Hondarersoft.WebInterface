using System;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;

namespace Hondarersoft.Utility.Extensions
{
    /// <summary>
    /// <see cref="ClientWebSocket"/> の拡張メソッドを提供します。
    /// </summary>
    public static class ClientWebSocketExtensions
    {
        /// <summary>
        /// Connect to a WebSocket server as an asynchronous operation.
        /// </summary>
        /// <param name="clientWebSocket">対象の <see cref="ClientWebSocket"/>。</param>
        /// <param name="uri">The URI of the WebSocket server to connect to.</param>
        /// <param name="timeout">タイムアウト監視を行う時間間隔。</param>
        /// <returns>Returns <see cref="Task"/>. The task object representing the asynchronous operation.</returns>
        public static async Task ConnectAsync(this ClientWebSocket clientWebSocket, Uri uri, TimeSpan timeout)
        {
            CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();
            cancellationTokenSource.CancelAfter(timeout);

            await clientWebSocket.ConnectAsync(uri, cancellationTokenSource.Token);
        }

        /// <summary>
        /// Connect to a WebSocket server as an asynchronous operation.
        /// </summary>
        /// <param name="clientWebSocket">対象の <see cref="ClientWebSocket"/>。</param>
        /// <param name="uri">The URI of the WebSocket server to connect to.</param>
        /// <param name="millisecondsTimeout">タイムアウト監視を行う時間。ミリ秒。</param>
        /// <returns>Returns <see cref="Task"/>. The task object representing the asynchronous operation.</returns>
        public static Task ConnectAsync(this ClientWebSocket clientWebSocket, Uri uri, int millisecondsTimeout)
        {
            return ConnectAsync(clientWebSocket, uri, TimeSpan.FromMilliseconds(millisecondsTimeout));
        }
    }
}
