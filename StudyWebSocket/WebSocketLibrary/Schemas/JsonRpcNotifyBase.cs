using System;
using System.Collections.Generic;
using System.Text;

namespace WebSocketLibrary.Schemas
{
    public class JsonRpcNotifyBase
    {
        public string Method { get; set; }
        public string Version { get; set; }
    }
}
