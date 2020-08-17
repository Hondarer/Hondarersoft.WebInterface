using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json.Serialization;

namespace WebInterfaceLibrary.Schemas
{
    public class JsonRpcResponse : JsonRpc
    {
        [JsonPropertyName("id")]
        public object Id { get; set; }

        [JsonPropertyName("result")]

        public object Result { get; set; } = null;

        [JsonPropertyName("error")]

        public Error Error { get; set; } = null;
    }
}
