using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json.Serialization;

namespace Hondarersoft.WebInterface.Schemas
{
    public class RestErrorResponse
    {
        [JsonPropertyName("error")]
        public object Error { get; set; }
    }
}
