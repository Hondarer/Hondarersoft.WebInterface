using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json.Serialization;

namespace Hondarersoft.WebInterface.Schemas
{
    public class ErrorWithData : Error
    {
        [JsonPropertyName("data")]
        public object Data { get; set; }
    }
}
