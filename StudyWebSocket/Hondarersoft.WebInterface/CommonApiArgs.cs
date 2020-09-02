using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.Json;

namespace Hondarersoft.WebInterface
{
    // WebSocket(JSON-RPC 2.0)およびWebAPI(REST WebAPI)の双方の要求を汎化して
    // 統一したインターフェースでAPIを実装するための引数。
    public class CommonApiArgs : EventArgs
    {
        public object Identifier { get; }

        public CommonApiMethods Method { get; }

        public Dictionary<string, string> RegExMatchGroups { get; internal set; } = null;

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

        public void SetError(Errors error)
        {
            SetError(error, error.ToString());
        }

        public void SetError(Errors error, string message)
        {
            Error = error;
            ErrorMessage = message;
            Handled = true;
        }

        public void SetException(Exception ex)
        {
#if DEBUG
            SetError(Errors.InternalError, ex.ToString());
#else
            SetError(Errors.InternalError, ex.Message);
#endif
        }

        public string Path { get; }

        public string RequestBody { get; }

        private object responseBody = null;

        public object ResponseBody 
        {
            get
            {
                return responseBody;
            }
            set
            {
                Handled = true;
                responseBody = value;
            }
        }

        /// <summary>
        /// この要求を処理したことを取得または設定します。
        /// <see cref="ResponseBody"/> プロパティを設定した場合、本プロパティは <c>true</c> に設定されます。
        /// レスポンスが無いメソッドの処理完了を設定するときは、明示的に本プロパティを <c>true</c> に設定してください。
        /// </summary>
        public bool Handled { get; set; } = false;

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
