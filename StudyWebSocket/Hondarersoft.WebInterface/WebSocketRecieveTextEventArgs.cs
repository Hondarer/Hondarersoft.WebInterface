using System;
using System.Collections.Generic;
using System.Text;

namespace Hondarersoft.WebInterface
{
    public class WebSocketRecieveTextEventArgs : WebSocketEventArgs
    {
        public string WebSocketIdentify { get; }

        public string Message { get; }

        public WebSocketRecieveTextEventArgs(string webSocketIdentify, string message) : base(webSocketIdentify)
        {
            Message = message;
        }
    }
}
