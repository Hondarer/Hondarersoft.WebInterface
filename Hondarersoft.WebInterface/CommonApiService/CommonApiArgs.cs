using System;
using System.Collections.Generic;
using System.Text.Json;

namespace Hondarersoft.WebInterface
{
    // WebSocket(JSON-RPC 2.0)および RESTful API の双方の要求を汎化して
    // 統一したインターフェースでAPIを実装するための引数。
    public class CommonApiArgs : EventArgs
    {
        public object Identifier { get; }

        public CommonApiMethods Method { get; }

        public string Path { get; }

        public Dictionary<string, string> RegExMatchGroups { get; internal set; } = null;

        public string RequestBody { get; }

        /// <summary>
        /// この要求を処理したことを取得または設定します。
        /// <see cref="ResponseBody"/> プロパティを設定した場合、本プロパティは <c>true</c> に設定されます。
        /// レスポンスが無いメソッドの処理完了を設定するときは、明示的に本プロパティを <c>true</c> に設定してください。
        /// </summary>
        public bool Handled { get; set; } = false;

        private object _responseBody = null;

        public object ResponseBody
        {
            get
            {
                return _responseBody;
            }
            set
            {
                Handled = true;
                _responseBody = value;
            }
        }

        public enum Errors : int
        {
            /// <summary>
            /// No error.
            /// </summary>
            None,

            /// <summary>
            /// Invalid JSON was received by the server.
            /// An error occurred on the server while parsing the JSON text.
            /// </summary>
            ParseError,

            /// <summary>
            /// The JSON sent is not a valid Request object.
            /// </summary>
            InvalidRequest,

            /// <summary>
            /// The method does not exist.
            /// </summary>
            MethodNotFound,

            /// <summary>
            /// The method is not available.
            /// </summary>
            MethodNotAvailable,

            /// <summary>
            /// Invalid method parameter(s).
            /// </summary>
            InvalidParams,

            /// <summary>
            /// Internal JSON-RPC error.
            /// </summary>
            InternalError,

            ///// <summary>
            ///// Reserved for implementation-defined server-errors.
            ///// </summary>
            //ServerError
        }

        public Errors Error { get; protected set; }

        public string ErrorMessage { get; protected set; }

        public object ErrorDetails { get; protected set; }

        public void SetError(Errors error)
        {
            SetError(error, error.ToString());
        }

        public void SetError(Errors error, string message, object errorDetails = null)
        {
            Error = error;
            ErrorMessage = message;
            ErrorDetails = errorDetails;
            Handled = true;
        }

        public void SetException(Exception ex)
        {
            SetError(Errors.InternalError, $"{Errors.InternalError}: {ex.GetType().Name}", ex.ToString());
        }

        public CommonApiArgs(object identifier, CommonApiMethods method, string path, string requestBody = null)
        {
            Identifier = identifier;
            Method = method;
            Path = path;
            RequestBody = requestBody;
        }

        public override string ToString()
        {
            return JsonSerializer.Serialize(this);
        }
    }
}
