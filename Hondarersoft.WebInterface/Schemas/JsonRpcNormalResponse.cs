using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json.Serialization;

namespace Hondarersoft.WebInterface.Schemas
{
    public class JsonRpcNormalResponse : JsonRpc
    {
        [JsonPropertyName("id")]
        public object Id { get; set; }

        [JsonPropertyName("result")]

        public object Result { get; set; }
    }
}
