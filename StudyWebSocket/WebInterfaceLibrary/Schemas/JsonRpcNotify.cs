using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json.Serialization;

namespace WebInterfaceLibrary.Schemas
{
    public class JsonRpcNotify : JsonRpc
    {
        [JsonPropertyName("method")]
        public string Method { get; set; }

        [JsonPropertyName("params")]
        public object Params { get; set; }
    }
}
