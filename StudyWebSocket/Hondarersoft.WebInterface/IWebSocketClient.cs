using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace Hondarersoft.WebInterface
{
    public interface IWebSocketClient
    {
        public Task ConnectAsync();

        public Task CloseAsync();

        public Task SendTextAsync(string message);

        public Task SendJsonAsync(object message, JsonSerializerOptions options = null);
    }
}
