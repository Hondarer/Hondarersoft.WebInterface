using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json.Serialization;

namespace WebSocketLibrary.Schemas
{
    public abstract class JsonRpc
    {
        public const string JSONRPC_VERSION = "2.0";

        [JsonPropertyName("jsonrpc")]
        public string Version { get; } = JSONRPC_VERSION;
    }
}
