// https://qiita.com/suin/items/f7ac4de914e9f3f35884

using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json.Serialization;

namespace Hondarersoft.WebInterface.Schemas
{
    public class Error
    {
        [JsonPropertyName("code")]
        public int Code { get; set; }

        [JsonPropertyName("message")]
        public string Message { get; set; }
    }
}
