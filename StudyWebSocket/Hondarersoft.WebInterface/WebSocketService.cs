// [C#]System.Net.WebSocketsを試す。その２。サーバー編。
// http://kimux.net/?p=956

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
    public class WebSocketService : WebSocketBase
    {
        private HttpListener httpListener = null; // TODO: Stop対応、稼働中の再スタート、Disopseの対応など

        public int MaxClients { get; set; } = int.MaxValue;

        public WebSocketService() : base()
        {
            Hostname = "+";
        }

        /// <summary>
        /// WebSocketサーバースタート
        /// </summary>
        public override void Start()
        {
            base.Start();

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

            ProcessHttpRequest();
        }

        protected async void ProcessHttpRequest()
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

                    Console.WriteLine("{0}:New Session:{1}", DateTime.Now.ToString(), listenerContext.Request.RemoteEndPoint.Address.ToString());
                    WebSocket websocket = (await listenerContext.AcceptWebSocketAsync(subProtocol: null)).WebSocket;

                    ProcessRecieve(Guid.NewGuid().ToString(), websocket);
                }
                else
                {
                    /// httpレスポンスを返す
                    listenerContext.Response.StatusCode = 400;
                    listenerContext.Response.Close();
                }
            }
        }
    }
}
