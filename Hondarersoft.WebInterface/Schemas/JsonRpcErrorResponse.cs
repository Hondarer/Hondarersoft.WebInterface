using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json.Serialization;

namespace Hondarersoft.WebInterface.Schemas
{
    public class JsonRpcErrorResponse : JsonRpc
    {
        [JsonPropertyName("id")]
        public object Id { get; set; }

        [JsonPropertyName("error")]

        public Error Error { get; set; }
    }
}
