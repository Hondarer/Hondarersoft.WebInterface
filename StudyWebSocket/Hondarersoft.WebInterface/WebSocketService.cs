// [C#]System.Net.WebSocketsを試す。その２。サーバー編。
// http://kimux.net/?p=956

using Hondarersoft.Utility;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Net;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace Hondarersoft.WebInterface
{
    public class WebSocketService : WebSocketBase, IWebSocketService
    {
        private HttpListener httpListener = null; // TODO: Stop対応、稼働中の再スタート、Disopseの対応など

        public int MaxClients { get; set; } = int.MaxValue;

        public WebSocketService(ILogger<WebSocketService> logger) : base(logger)
        {
            Hostname = "+";
        }

        /// <summary>
        /// WebSocketサーバースタート
        /// </summary>
        public override async Task StartAsync()
        {
            await base.StartAsync();

            if ((string.IsNullOrEmpty(Hostname) == true) ||
                (PortNumber == 0) ||
                (string.IsNullOrEmpty(BasePath) == true))
            {
                throw new Exception("invalid endpoint parameter");
            }

            // httpListenerで待ち受け
            httpListener = new HttpListener();

            string ssl = string.Empty;
            if (UseSSL == true)
            {
                ssl = "s";
            }
            httpListener.Prefixes.Add($"http{ssl}://{Hostname}:{PortNumber}/{BasePath}/");

            //httpListener.Prefixes.Add("http://+:8000/ws/");
            httpListener.Start();

            ProcessHttpRequest().NoWait();
        }

        protected async Task ProcessHttpRequest()
        {
            while (httpListener.IsListening == true)
            {
                // 接続待機
                HttpListenerContext listenerContext = await httpListener.GetContextAsync();

                if (httpListener.IsListening == false)
                {
                    break;
                }

                if (listenerContext.Request.IsWebSocketRequest)
                {
                    // httpのハンドシェイクがWebSocketならWebSocket接続開始

                    if (webSockets.Count >= MaxClients)
                    {
                        // 接続数オーバー
                        listenerContext.Response.StatusCode = 400;
                        listenerContext.Response.Close();
                    }

                    string webSocketIdentify = Guid.NewGuid().ToString();

                    _logger.LogInformation("WebSocketRequest from {0}, accept. webSocketIdentify = {1}.", listenerContext.Request.RemoteEndPoint.Address.ToString(), webSocketIdentify);
                    WebSocket websocket = (await listenerContext.AcceptWebSocketAsync(subProtocol: null)).WebSocket;

                    ProcessRecieve(webSocketIdentify, websocket).NoWait();
                }
                else
                {
                    // httpレスポンスを返す
                    listenerContext.Response.StatusCode = 400;
                    listenerContext.Response.Close();
                }
            }
        }
    }
}
