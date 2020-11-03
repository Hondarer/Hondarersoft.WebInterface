using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json.Serialization;

namespace Hondarersoft.WebInterface.Schemas
{
    public class RestNormalResponse
    {
        [JsonPropertyName("result")]
        public object Result { get; set; }
    }
}
