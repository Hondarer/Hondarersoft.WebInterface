using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json.Serialization;

namespace WebSocketLibrary.Schemas
{
    public class JsonRpcErrorResponse : JsonRpcResponseBase
    {
        [JsonPropertyName("error")]

        public Error Error { get; set; }
    }
}
