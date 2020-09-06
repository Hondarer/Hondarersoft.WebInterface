using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace Hondarersoft.WebInterface
{
    public interface IWebSocketBase : IWebInterfaceService
    {
        public event EventHandler<WebSocketRecieveTextEventArgs> WebSocketRecieveText;

        public IReadOnlyList<string> WebSocketIdentifies { get; }

        public Task SendTextAsync(string webSocketIdentify, string message);

        public Task SendJsonAsync(string webSocketIdentify, object message, JsonSerializerOptions options = null);
    }
}
