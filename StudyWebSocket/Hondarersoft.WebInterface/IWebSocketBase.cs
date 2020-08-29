using System;
using System.Collections.Generic;
using System.Text;

namespace Hondarersoft.WebInterface
{
    public interface IWebSocketBase
    {
        public class WebSocketRecieveTextEventArgs : EventArgs
        {
            public string WebSocketIdentify { get; }

            public string Message { get; }

            public WebSocketRecieveTextEventArgs(string webSocketIdentify, string message)
            {
                WebSocketIdentify = webSocketIdentify;
                Message = message;
            }
        }

        public delegate void WebSocketRecieveTextHandler(object sender, WebSocketRecieveTextEventArgs e);
        public event WebSocketRecieveTextHandler WebSocketRecieveText;
    }
}
