﻿using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json.Serialization;

namespace WebSocketLibrary.Schemas
{
    public class JsonRpcRequest : JsonRpcNotify
    {
        [JsonPropertyName("id")]
        public object Id { get; set; }
    }
}