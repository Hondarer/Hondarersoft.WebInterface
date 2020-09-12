using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json.Serialization;

namespace Hondarersoft.WebInterface.Schemas
{
    public class JsonRpcErrorResponseWithData : JsonRpc
    {
        [JsonPropertyName("id")]
        public object Id { get; set; }

        [JsonPropertyName("error")]

        public new ErrorWithData Error { get; set; }
    }
}
