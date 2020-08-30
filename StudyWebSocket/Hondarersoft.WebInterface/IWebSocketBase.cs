using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

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

        public IReadOnlyList<string> WebSocketIdentifies { get; }

        public Task SendTextAsync(string webSocketIdentify, string message);

        public Task SendJsonAsync(string webSocketIdentify, object message, JsonSerializerOptions options = null);
    }
}
