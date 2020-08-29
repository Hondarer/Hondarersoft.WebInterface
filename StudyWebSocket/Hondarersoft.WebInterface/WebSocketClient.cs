using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace Hondarersoft.WebInterface
{
    public class WebSocketClient : WebSocketBase, IWebSocketClient, IWebInteraceProxySetting
    {
        protected ClientWebSocket websocket = null;

        #region IWebInteraceProxySetting Implements

        private bool _useDefaultProxy = false;

        public bool UseDefaultProxy
        {
            get
            {
                return _useDefaultProxy;
            }
            set
            {
                _useDefaultProxy = value;
                if (value == true)
                {
                    UseCustomProxy = false;
                }
            }
        }

        private bool _useCustomProxy = false;

        public bool UseCustomProxy
        {
            get
            {
                return _useCustomProxy;
            }
            set
            {
                _useCustomProxy = value;
                if (value == true)
                {
                    UseDefaultProxy = false;
                }
            }
        }

        public string ProxyUrl { get; set; } = null;

        public string ProxyAccount { get; set; } = null;

        public string ProxyPassword { get; set; } = null;

        #endregion

        public WebSocketClient(ILogger<WebSocketClient> logger) : base(logger)
        {
        }

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

                    ClientWebSocketOptions options = websocket.Options;

                    if (UseDefaultProxy == false)
                    {
                        if (UseCustomProxy == true)
                        {
                            options.Proxy = new WebProxy(ProxyUrl)
                            {
                                Credentials = new NetworkCredential(ProxyAccount, ProxyPassword)
                            };
                        }
                        else
                        {
                            // 引数なしの WebProxy は、直接接続を提供する。
                            options.Proxy = new WebProxy();
                        }
                    }

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

            ProcessRecieve(Guid.NewGuid().ToString(), websocket);
        }

        public async Task CloseAsync()
        {
            if ((websocket != null) && ((websocket.State == WebSocketState.Connecting) || (websocket.State == WebSocketState.Open)))
            {
                await websocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "The socket was closed via the CloseAsync method.", CancellationToken.None);
                websocket = null;
            }
        }

        public async Task SendTextAsync(string message)
        {
            string webSocketIdentify = webSockets.Keys.FirstOrDefault();
            // TODO: 未接続だとnullになる。エラー処理要。

            await SendTextAsync(webSocketIdentify, message);
        }

        public async Task SendJsonAsync(object message, JsonSerializerOptions options = null)
        {
            if (options == null)
            {
                options = new JsonSerializerOptions
                {
                    IgnoreNullValues = true
                };
            }

            string webSocketIdentify = webSockets.Keys.FirstOrDefault();
            // TODO: 未接続だとnullになる。エラー処理要。

            await SendJsonAsync(webSocketIdentify, message, options);
        }
    }
}
