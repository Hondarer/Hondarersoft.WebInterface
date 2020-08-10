using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json.Serialization;

namespace WebSocketLibrary.Schemas
{
    class JsonRpcNormalResponse
    {
        [JsonPropertyName("data")]

        public object Data { get; set; }

    }
}
