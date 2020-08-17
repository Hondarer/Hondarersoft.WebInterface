using System;
using System.Collections.Generic;
using System.Dynamic;
using System.IO;
using System.Net;
using System.Net.WebSockets;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using WebInterfaceLibrary.Controllers;
using WebInterfaceLibrary.Schemas;

namespace WebInterfaceLibrary
{
    public class CommonApiManager
    {
        private const string CONTENT_TYPE_JSON = "application/json";

        protected static readonly Dictionary<CommonApiArgs.Errors, int> ErrorsToCode = new Dictionary<CommonApiArgs.Errors, int>()
        {
            {CommonApiArgs.Errors.ParseError,-32700},
            {CommonApiArgs.Errors.InvalidRequest,-32600},
            {CommonApiArgs.Errors.MethodNotFound,-32601}, // same
            {CommonApiArgs.Errors.MethodNotAvailable,-32601}, // same
            {CommonApiArgs.Errors.InvalidParams,-32602},
            {CommonApiArgs.Errors.InternalError,-32603},
        };

        // 各サーバー、クライアントを登録可能にする
        // 登録されたときに受信イベントをハンドリングする
        // 受信イベントに応じた応答を返却する
        // 送信イベントも同様にここで握る

        private WebApiService webApiService;
        private WebSocketService webSocketService;

        public void Start()
        {
            // TODO: 以下はとりあえずの動作確認

            webApiService = new WebApiService()
            {
                AllowCORS = true
            };

            webApiService.WebApiRequest += WebApiService_WebApiRequest;

            webApiService.Start();

            webSocketService = new WebSocketService();

            webSocketService.WebSocketRecieveText += WebSocketService_WebSocketRecieveText;

            webSocketService.Start();
        }

        private async void WebSocketService_WebSocketRecieveText(object sender, WebSocketBase.WebSocketRecieveTextEventArgs e)
        {
            // TODO: バッチ処理に対応していない(仕様にはあるが必要性は疑問)
            //       受信したデータがいきなり配列の場合はバッチ処理

            WebSocket webSocket = sender as WebSocket;
            dynamic document;

            try
            {
                document = JsonSerializer.Deserialize<ExpandoObject>(e.Message);
            }
            catch
            {
                // この段階の例外は応答する術がない
                return;
            }

            object response;

            // jsonrpc メンバーのチェック
            if ((DynamicHelper.IsPropertyExist(document, "jsonrpc") == false) || 
                (document.jsonrpc.ToString() != "2.0"))
            {
                // NOP(レスポンスを返すことができない)
                return;
            }

            // id メンバーの取り出し
            object id = null;
            JsonElement idElement = document.id;
            if (DynamicHelper.IsPropertyExist(document, "id") == true)
            {
                if (idElement.ValueKind == JsonValueKind.Number)
                {
                    // 規約上、Numbers SHOULD NOT contain fractional parts
                    // とあるので、整数として扱う。少数の場合は動作不定となる。
                    id = idElement.GetInt64();
                }
                else if (idElement.ValueKind == JsonValueKind.String)
                {
                    id = idElement.GetString();
                }
                else
                {
                    // id フィールドのフォーマット不良。
                    // レスポンスを返すことができない、受け取ったレスポンスを処理することができない。
                    // この場合は処理をしない。

                    // 規約上は id フィールドは null も許容されるが、本実装では許容しない。
                    // (id が特定できなかった際のレスポンスには、id が null のレスポンスを送ることになっている。
                    //  本実装ではこの処理も省略している。)
                    return;
                }
            }

            // result か error があったら応答電文
            if ((DynamicHelper.IsPropertyExist(document, "result") == true) || (DynamicHelper.IsPropertyExist(document, "error") == true))
            {
                // 応答電文の処理

                // TODO: 処理未実装
                return;
            }

            // メソッド名の特定
            // JSON-RPC メソッド名の末尾を HTTP メソッドとして取り出し、
            // ドット結合をピリオド結合にし、ルート記号を付加する
            string jsonrpcMethod = document.method.ToString();
            CommonApiArgs.Methods method = CommonApiArgs.Methods.UNKNOWN;
            string path = string.Empty;
            foreach(string methodEnum in Enum.GetNames(typeof(CommonApiArgs.Methods)))
            {
                if (jsonrpcMethod.EndsWith("." + methodEnum.ToLower()) == true)
                {
                    method = (CommonApiArgs.Methods)Enum.Parse(typeof(CommonApiArgs.Methods), methodEnum);
                    path = "/" + jsonrpcMethod.Substring(0, jsonrpcMethod.Length - methodEnum.Length-1).Replace('.', '/');
                    break;
                }
            }

            // HTTP メソッド名で終わっていない JSON-RPC メソッド名は、エラーとして扱う
            if (method == CommonApiArgs.Methods.UNKNOWN)
            {
                if (id != null)
                {
                    response = new JsonRpcResponse() { Id = id, Error = new Error() { Code = ErrorsToCode[CommonApiArgs.Errors.MethodNotFound], Message = CommonApiArgs.Errors.MethodNotFound.ToString() } };
                    JsonSerializerOptions options = new JsonSerializerOptions
                    {
                        IgnoreNullValues = true
                    };
                    await webSocketService.SendJsonAsync(response, webSocket, options);
                }
                return;
            }

            object paramsValue = null;
            if (DynamicHelper.IsPropertyExist(document, "params") == true)
            {
                paramsValue = DynamicHelper.GetProperty(document, "params");
            }
            if (paramsValue != null)
            {
                paramsValue = paramsValue.ToString();
            }

            CommonApiArgs apiArgs =
                new CommonApiArgs(id, method, path, (string)paramsValue);

            OnRequest(apiArgs);

            if (id != null)
            {
                // 応答用の id が存在する場合に返送処理を行う

                if (apiArgs.Error == CommonApiArgs.Errors.None)
                {
                    response = new JsonRpcResponse() { Id = apiArgs.Identifier, Result = apiArgs.ResponseBody };
                }
                else
                {
                    int code = ErrorsToCode[CommonApiArgs.Errors.InternalError];
                    if (ErrorsToCode.ContainsKey(apiArgs.Error) == true)
                    {
                        code = ErrorsToCode[apiArgs.Error];
                    }

                    response = new JsonRpcResponse() { Id = apiArgs.Identifier, Error = new Error() { Code = code, Message = apiArgs.ErrorMessage } };
                }

                JsonSerializerOptions options = new JsonSerializerOptions
                {
                    IgnoreNullValues = true
                };
                await webSocketService.SendJsonAsync(response, webSocket, options);
            }
        }

        /// <summary>
        /// APIパスを取得する
        /// </summary>
        /// <param name="srcPath">URLパス</param>
        /// <returns>APIパス</returns>
        private string GetApiPath(string srcPath)
        {
            string[] path = srcPath.Split('?');
            string condition = String.Format(@"^/{0}", "Temporary_Listen_Addresses/v1.0");
            //string condition = String.Format(@"^/{0}", Settings.Default.API_PATH);
            return Regex.Replace(path[0], condition, "");
        }

        private void WebApiService_WebApiRequest(object sender, WebApiService.WebApiRequestEventArgs e)
        {
            StreamReader reader = null;
            StreamWriter writer = null;

            try
            {
                e.Response.ContentType = CONTENT_TYPE_JSON;
                e.Response.ContentEncoding = Encoding.UTF8;

                reader = new StreamReader(e.Request.InputStream);
                writer = new StreamWriter(e.Response.OutputStream);
                string reqBody = reader.ReadToEnd();

                string path = GetApiPath(e.Request.RawUrl);

                // TODO: QueryString も渡せるようにしたほうがいい
                //Console.WriteLine(req.QueryString.Get("hhh"));

                CommonApiArgs commonApiArgs =
                    new CommonApiArgs(e.Request.RequestTraceIdentifier, Enum.Parse<CommonApiArgs.Methods>(e.Request.HttpMethod), path, reqBody);

                OnRequest(commonApiArgs);

                if (commonApiArgs.Error == CommonApiArgs.Errors.None)
                {
                    e.Response.StatusCode = (int)HttpStatusCode.OK;
                    writer.BaseStream.Write(JsonSerializer.SerializeToUtf8Bytes(commonApiArgs.ResponseBody));
                }
                else
                {
                    int code = ErrorsToCode[CommonApiArgs.Errors.InternalError];
                    if (ErrorsToCode.ContainsKey(commonApiArgs.Error) == true)
                    {
                        code = ErrorsToCode[commonApiArgs.Error];
                    }

                    switch (commonApiArgs.Error)
                    {
                        case CommonApiArgs.Errors.ParseError:
                        // No Break
                        case CommonApiArgs.Errors.InvalidRequest:
                        // No Break
                        case CommonApiArgs.Errors.InvalidParams:
                            e.Response.StatusCode = (int)HttpStatusCode.BadRequest;
                            break;
                        case CommonApiArgs.Errors.MethodNotFound:
                            e.Response.StatusCode = (int)HttpStatusCode.NotFound;
                            break;
                        case CommonApiArgs.Errors.InternalError:
                            e.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
                            break;
                        case CommonApiArgs.Errors.MethodNotAvailable:
                            e.Response.StatusCode = (int)HttpStatusCode.MethodNotAllowed;
                            break;
                        default:
                            // 下記は念のため(本来通過することはない)
                            e.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
                            break;
                    }

                    writer.BaseStream.Write(JsonSerializer.SerializeToUtf8Bytes(new Error() { Code = code, Message = commonApiArgs.ErrorMessage }));
                }
            }
            catch (Exception ex)
            {
                //resBoby = CreateErrorResponse(ErrorCode.SYSTEM_ERROR, String.Format(Resources.ErrorUnexpected, ex.Message));
                e.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
                //log.Error(ex.ToString());
            }
            finally
            {
                try
                {
                    //writer.Write(resBoby);
                    writer.Flush();

                    if (null != reader)
                    {
                        reader.Close();
                    }
                    if (null != writer)
                    {
                        writer.Close();
                    }
                }
                catch (Exception clsEx)
                {
                    //log.Error(clsEx.ToString());
                }
            }
        }

        public virtual void OnRequest(CommonApiArgs apiArgs)
        {
            // メソッドとパスを使って分岐させて呼び出す。

            // TODO: 実装は検証用の決め打ち処理になっている。

            CpuModesController cpuModesController = new CpuModesController();

            cpuModesController.Get(apiArgs);

            if (apiArgs.Handled == false)
            {
                apiArgs.SetError(CommonApiArgs.Errors.MethodNotFound);
            }
        }

    }
}
