using System;
using System.Collections.Generic;
using System.Text;

namespace WebSocketLibrary.Schemas
{
    public class JsonRpcResponseBase
    {
        public object Id { get; set; }
        public string Version { get; set; }

    }
}
