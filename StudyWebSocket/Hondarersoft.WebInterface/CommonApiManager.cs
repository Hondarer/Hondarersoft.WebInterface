using Hondarersoft.WebInterface.Controllers;
using Hondarersoft.WebInterface.Schemas;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Dynamic;
using System.IO;
using System.Net;
using System.Net.WebSockets;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Linq;

namespace Hondarersoft.WebInterface
{
    public class CommonApiManager : ICommonApiManager
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

        private readonly IServiceProvider serviceProvider;

        public CommonApiManager(IServiceProvider serviceProvider)
        {
            this.serviceProvider = serviceProvider;
        }

        protected readonly List<WebInterfaceBase> webInterfaceBasees = new List<WebInterfaceBase>();

        protected readonly List<ICommonApiController> commonApiControllers = new List<ICommonApiController>();

        public ICommonApiManager Start()
        {
            foreach (var webInterfaceBase in webInterfaceBasees)
            {
                if (webInterfaceBase is IWebInterfaceService)
                {
                    (webInterfaceBase as IWebInterfaceService).Start();
                }
            }

            return this;
        }

        public ICommonApiManager RegistController(string assemblyName, string classFullName)
        {
            Assembly asm = Assembly.Load(assemblyName);
            Type commonApiControllerType = asm.GetType(classFullName);

            // 各インターフェースは DI コンテナから払い出したいので、ここで払い出し処理を行う。
            // TODO: 各種例外への対応ができていない。
            // TODO: 処理を別クラスに切り出したほうがいい。

            List<Type> types = new List<Type>();
            List<object> objects = new List<object>();

            foreach (ParameterInfo parameter in commonApiControllerType.GetConstructors().FirstOrDefault().GetParameters())
            {
                types.Add(parameter.ParameterType);
                objects.Add(serviceProvider.GetService(parameter.ParameterType));
            }
            ConstructorInfo constructor = commonApiControllerType.GetConstructor(types.ToArray());
            ICommonApiController commonApiController = constructor.Invoke(objects.ToArray()) as ICommonApiController;

            commonApiControllers.Add(commonApiController);

            return this;
        }

        public ICommonApiManager RegistInterface(WebInterfaceBase webInterfaceBase)
        {
            if (webInterfaceBase is WebApiService) // TODO: インターフェース化
            {
                (webInterfaceBase as WebApiService).WebApiRequest += WebApiService_WebApiRequest;
            }
            if (webInterfaceBase is WebSocketBase) // TODO: インターフェース化
            {
                (webInterfaceBase as WebSocketBase).WebSocketRecieveText += WebSocketService_WebSocketRecieveText;
            }

            webInterfaceBasees.Add(webInterfaceBase);

            return this;
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
            foreach (string methodEnum in Enum.GetNames(typeof(CommonApiArgs.Methods)))
            {
                if (jsonrpcMethod.EndsWith("." + methodEnum.ToLower()) == true)
                {
                    method = (CommonApiArgs.Methods)Enum.Parse(typeof(CommonApiArgs.Methods), methodEnum);
                    path = "/" + jsonrpcMethod.Substring(0, jsonrpcMethod.Length - methodEnum.Length - 1).Replace('.', '/');
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
                    await WebSocketBase.SendJsonAsync(response, webSocket, options);
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

                // TODO: このタイミングで例外が発生しうる。その場合は何もできないので、ここで握りつぶす。
                await WebSocketBase.SendJsonAsync(response, webSocket, options);
            }
        }

        /// <summary>
        /// APIパスを取得する
        /// </summary>
        /// <param name="srcPath">URLパス</param>
        /// <returns>APIパス</returns>
        private string GetApiPath(string srcPath, string basePath)
        {
            string[] path = srcPath.Split('?');
            string condition = String.Format(@"^/{0}", basePath);
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

                string path = GetApiPath(e.Request.RawUrl, (sender as IWebInterfaceService).BasePath);

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
            // 登録されているコントローラーでループする。
            foreach (var commonApiController in commonApiControllers)
            {
                // パスが前方一致したコントローラーの指定したメソッドを呼び出す。
                if (apiArgs.Path.StartsWith(commonApiController.ApiPath) == true)
                {
                    // TODO: 例外を拾って、例外の場合はエラーを設定する。
                    switch (apiArgs.Method)
                    {
                        case CommonApiArgs.Methods.GET:
                            commonApiController.Get(apiArgs);
                            break;
                        case CommonApiArgs.Methods.POST:
                            commonApiController.Post(apiArgs);
                            break;
                        case CommonApiArgs.Methods.PUT:
                            commonApiController.Put(apiArgs);
                            break;
                        case CommonApiArgs.Methods.DELETE:
                            commonApiController.Delete(apiArgs);
                            break;
                        default:
                            break;
                    }

                    // 処理完了していたら、他のコントローラーのチェックは行わない。
                    if (apiArgs.Handled == true)
                    {
                        break;
                    }
                }
            }

            // どのコントローラーも処理しなかった場合は、エラーを返す。
            if (apiArgs.Handled == false)
            {
                apiArgs.SetError(CommonApiArgs.Errors.MethodNotFound);
            }
        }

    }
}
