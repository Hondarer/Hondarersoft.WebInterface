using System;
using System.Collections.Generic;
using System.Text;

namespace Hondarersoft.WebInterface
{
    public class WebSocketEventArgs : EventArgs
    {
        public string WebSocketIdentify { get; }

        public WebSocketEventArgs(string webSocketIdentify)
        {
            WebSocketIdentify = webSocketIdentify;
        }
    }
}
