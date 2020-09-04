using Hondarersoft.Utility;
using Hondarersoft.WebInterface.Controllers;
using Hondarersoft.WebInterface.Schemas;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Dynamic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Hondarersoft.Hosting;
using System.Text.RegularExpressions;
using Hondarersoft.Utility.Extensions;

namespace Hondarersoft.WebInterface
{
    public class CommonApiService : ICommonApiService // TODO: IDisposable 化
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

        private readonly IServiceProvider _serviceProvider = null;
        private readonly ILogger _logger = null;

        public CommonApiService(IServiceProvider serviceProvider, ILogger<CommonApiService> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        protected readonly Dictionary<string, IWebInterface> webInterfaces = new Dictionary<string, IWebInterface>();

        protected readonly Dictionary<IWebInterface, string> webInterfaceIdentities = new Dictionary<IWebInterface, string>();


        public IReadOnlyList<string> WebInterfaceIdentifies
        {
            get
            {
                return webInterfaces.Keys.ToList();
            }
        }

        protected readonly List<ICommonApiController> commonApiControllers = new List<ICommonApiController>();

        public async Task<ICommonApiService> StartAsync()
        {
            foreach (var webInterfaceBase in webInterfaces.Values)
            {
                if (webInterfaceBase is IWebInterfaceService)
                {
                    await (webInterfaceBase as IWebInterfaceService).StartAsync();
                }
            }

            return this;
        }

        public ICommonApiService RegistController(IConfiguration configurationRoot)
        {
            CommonApiControllerConfigEntry[] controllerConfig = configurationRoot.GetSection("CommonApiControllers").Get<CommonApiControllerConfigEntry[]>();

            if (controllerConfig == null)
            {
                return this;
            }

            foreach (CommonApiControllerConfigEntry entry in controllerConfig)
            {
                // TODO: 各種例外への対応ができていない。
                // TODO: 型があっていないと null になるのでチェック要
                ICommonApiController commonApiController = _serviceProvider.GetService(entry.AssemblyName, entry.ClassFullName, entry.IsSingleton) as ICommonApiController;
                commonApiControllers.Add(commonApiController);
            }

            return this;
        }

        public ICommonApiService RegistInterface(IWebInterface webInterfaceBase, string webInterfaceIdentify = null)
        {
            if (webInterfaceBase is IWebApiService)
            {
                (webInterfaceBase as IWebApiService).WebApiRequest += WebApiService_WebApiRequest;
            }
            if (webInterfaceBase is IWebSocketBase)
            {
                (webInterfaceBase as IWebSocketBase).WebSocketRecieveText += WebSocketService_WebSocketRecieveText;
            }

            if(webInterfaceIdentify == null)
            {
                webInterfaceIdentify = Guid.NewGuid().ToString();
            }

            // TODO: キー重複で例外が発生する。メソッド仕様に明記要。
            webInterfaces.Add(webInterfaceIdentify, webInterfaceBase);
            webInterfaceIdentities.Add(webInterfaceBase, webInterfaceIdentify);

            return this;
        }

        protected readonly Dictionary<string, CountdownEvent> _waitingReplyEvent = new Dictionary<string, CountdownEvent>();
        protected readonly Dictionary<string, CommonApiResponse> _waitingReplyData = new Dictionary<string, CommonApiResponse>();

        public async Task<CommonApiResponse> SendRequestAsync(CommonApiRequest request, string interfaceIdentify = null, string sessionIdentify = null)
        {
            CommonApiResponse response = new CommonApiResponse();

            if (string.IsNullOrEmpty(interfaceIdentify) == true)
            {
                if (webInterfaces.Count == 1)
                {
                    interfaceIdentify = webInterfaces.First().Key;
                }
            }

            // TODO: 辞書にない場合は適切な例外にする
            IWebInterface webInterface = webInterfaces[interfaceIdentify];

            if (webInterface is IWebApiClient)
            {
                HttpResponseMessage httpResponse = null;
                try
                {
                    switch(request.Method)
                    {
                        case CommonApiMethods.GET:
                            httpResponse = await (webInterface as IWebApiClient).GetAsync(request.Path, request.Timeout);
                            break;
                        default:
                            // 未実装
                            break;
                    }
                    if(httpResponse == null)
                    {
                        // 未実装メソッドの指定がなされた
                        return response;
                    }
                }
                catch
                {
                    // 接続できなかった
                    // ex) タイムアウトのとき: TaskCanceledException
                    return response;
                }

                if (httpResponse.IsSuccessStatusCode == true)
                {
                    // API 呼び出しに成功
                    string result;
                    try
                    {
                        result = await httpResponse.Content.ReadAsStringAsync();
                    }
                    catch
                    {
                        // Bodyの取得に失敗
                        return response;
                    }

                    response.ResponseBody = result;
                    response.IsSuccess = true;
                }
                else
                {
                    // API呼び出しに失敗
                    Error result;
                    try
                    {
                        result = await httpResponse.Content.ReadAsJsonAsync<Error>();
                    }
                    catch
                    {
                        // Bodyのデシリアライズに失敗
                        return response;
                    }

                    response.Error = result;
                }
            }
            else if (webInterface is IWebSocketBase)
            {
                string methodsName = request.Path.Replace("/", ".");
                methodsName += "." + request.Method.ToString().ToLower();
                if (methodsName.StartsWith(".") == true)
                {
                    methodsName = methodsName.Substring(1);
                }

                string requestId = null;
                CountdownEvent waitingEvent = null;

                JsonRpcNotify jsonRpcNotify;
                if (request.NotifyOnly != true)
                {
                    jsonRpcNotify = new JsonRpcRequest() { Method = methodsName, Params = request.RequestBody };
                    requestId = (jsonRpcNotify as JsonRpcRequest).Id.ToString();

                    waitingEvent = new CountdownEvent(1);
                    lock (this)
                    {
                        _waitingReplyEvent.Add(requestId, waitingEvent);
                    }
                }
                else
                {
                    jsonRpcNotify = new JsonRpcNotify() { Method = methodsName, Params = request.RequestBody };
                }

                try
                {
                    JsonSerializerOptions options = new JsonSerializerOptions
                    {
                        IgnoreNullValues = true
                    };

                    if (webInterface is IWebSocketClient)
                    {
                        await (webInterface as IWebSocketClient).SendJsonAsync(jsonRpcNotify, options);
                    }
                    else
                    {
                        if (string.IsNullOrEmpty(sessionIdentify) == true)
                        {
                            if ((webInterface as IWebSocketBase).WebSocketIdentifies.Count == 1)
                            {
                                sessionIdentify = (webInterface as IWebSocketBase).WebSocketIdentifies.First();
                            }
                        }

                        await (webInterface as IWebSocketBase).SendJsonAsync(sessionIdentify, jsonRpcNotify, options);
                    }
                }
                catch
                {
                    // 送信エラー
                    if (request.NotifyOnly != true)
                    {
                        lock (this)
                        {
                            if (_waitingReplyEvent.ContainsKey(requestId) == true)
                            {
                                _waitingReplyEvent.Remove(requestId);
                            }
                            if (_waitingReplyData.ContainsKey(requestId) == true)
                            {
                                _waitingReplyData.Remove(requestId);
                            }
                        }
                    }
                    return response;
                }

                if (request.NotifyOnly == true)
                {
                    response.IsSuccess = true;
                    return response;
                }

                try
                {
                    waitingEvent.Wait(request.Timeout); // TODO: タイムアウト時は例外出るか確認
                }
                finally
                {
                    lock (this)
                    {
                        if (_waitingReplyEvent.ContainsKey(requestId) == true)
                        {
                            _waitingReplyEvent.Remove(requestId);
                        }
                        if (_waitingReplyData.ContainsKey(requestId) == true)
                        {
                            response = _waitingReplyData[requestId];
                            _waitingReplyData.Remove(requestId);
                        }
                        else
                        {
                            // 応答タイムアウト
                        }
                    }
                }
            }
            else
            {
                // リクエストに対応しないインターフェースへの要求
                throw new InvalidOperationException();
            }

            return response;
        }

        public async Task<CommonApiResponse> SendRequestAsync<T>(CommonApiRequest request, string interfaceIdentify = null, string sessionIdentify = null)
        {
            CommonApiResponse response = await SendRequestAsync(request, interfaceIdentify, sessionIdentify);

            if((response.IsSuccess == true) && (response.ResponseBody != null))
            {
                try
                {
                    response.ResponseBody = JsonSerializer.Deserialize<T>(response.ResponseBody.ToString());
                }
                catch
                {
                    // Result のデシリアライズに失敗
                    response.IsSuccess = false;
                }
            }

            return response;
        }

        private void WebSocketService_WebSocketRecieveText(object sender, IWebSocketBase.WebSocketRecieveTextEventArgs e)
        {
            WebSocketService_WebSocketRecieveTextImpl(sender, e).NoWaitAndWatchException();
        }

        private async Task WebSocketService_WebSocketRecieveTextImpl(object sender, IWebSocketBase.WebSocketRecieveTextEventArgs e)
        {
            // TODO: バッチ処理に対応していない(仕様にはあるが必要性は疑問)
            //       受信したデータがいきなり配列の場合はバッチ処理

            WebSocketBase webSocketBase = sender as WebSocketBase;
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

                CommonApiResponse commonApiResponse = new CommonApiResponse();

                if (DynamicHelper.IsPropertyExist(document, "result") == true)
                {
                    commonApiResponse.ResponseBody = DynamicHelper.GetProperty(document, "result").ToString();

                    commonApiResponse.IsSuccess = true;
                }
                else if (DynamicHelper.IsPropertyExist(document, "error") == true)
                {
                    try
                    {
                        commonApiResponse.Error = JsonSerializer.Deserialize<Error>(DynamicHelper.GetProperty(document, "error").ToString());
                    }
                    catch
                    {
                        // Errorのパース失敗
                    }
                }
                else
                {
                    // 想定外
                }

                string idString = id.ToString();
                lock (this)
                {
                    if (_waitingReplyEvent.ContainsKey(idString) == true)
                    {
                        _waitingReplyData.Add(idString, commonApiResponse);
                        _waitingReplyEvent[idString].Signal();
                    }
                }

                return;
            }

            // メソッド名の特定
            // JSON-RPC メソッド名の末尾を HTTP メソッドとして取り出し、
            // ドット結合をピリオド結合にし、ルート記号を付加する
            string jsonrpcMethod = document.method.ToString();
            CommonApiMethods method = CommonApiMethods.UNKNOWN;
            string path = string.Empty;
            foreach (string methodEnum in Enum.GetNames(typeof(CommonApiMethods)))
            {
                if (jsonrpcMethod.EndsWith("." + methodEnum.ToLower()) == true)
                {
                    method = (CommonApiMethods)Enum.Parse(typeof(CommonApiMethods), methodEnum);
                    path = "/" + jsonrpcMethod.Substring(0, jsonrpcMethod.Length - methodEnum.Length - 1).Replace('.', '/');
                    break;
                }
            }

            // HTTP メソッド名で終わっていない JSON-RPC メソッド名は、エラーとして扱う
            if (method == CommonApiMethods.UNKNOWN)
            {
                if (id != null)
                {
                    response = new JsonRpcErrorResponse() { Id = id, Error = new Error() { Code = ErrorsToCode[CommonApiArgs.Errors.MethodNotFound], Message = CommonApiArgs.Errors.MethodNotFound.ToString() } };
                    await webSocketBase.SendJsonAsync(e.WebSocketIdentify, response);
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
                    response = new JsonRpcNormalResponse() { Id = apiArgs.Identifier, Result = apiArgs.ResponseBody };
                }
                else
                {
                    int code = ErrorsToCode[CommonApiArgs.Errors.InternalError];
                    if (ErrorsToCode.ContainsKey(apiArgs.Error) == true)
                    {
                        code = ErrorsToCode[apiArgs.Error];
                    }

                    response = new JsonRpcErrorResponse() { Id = apiArgs.Identifier, Error = new Error() { Code = code, Message = apiArgs.ErrorMessage } };
                }

                // TODO: このタイミングで例外が発生しうる。その場合は何もできないので、ここで握りつぶす。
                await webSocketBase.SendJsonAsync(e.WebSocketIdentify, response);
            }
        }

        private void WebApiService_WebApiRequest(object sender, IWebApiService.WebApiRequestEventArgs e)
        {
            StreamReader reader = null;
            StreamWriter writer = null;

            try
            {
                e.Response.ContentEncoding = Encoding.UTF8;

                reader = new StreamReader(e.Request.InputStream);
                writer = new StreamWriter(e.Response.OutputStream);
                string reqBody = reader.ReadToEnd();

                // クエリストリングを除いたパスの取得
                string path = e.Request.RawUrl.Split('?').First();

                // GET で クエリストリングが存在する場合に、
                // 内部処理では body に書かれたものとしてパラメーターを処理するため詰め替える。
                // パラメーターはキー名がメンバ名となり、値は常に string[] となる。
                if ((e.Request.HttpMethod == "GET") && (e.Request.QueryString.Count > 0))
                {
                    dynamic document = new ExpandoObject();

                    foreach (string key in e.Request.QueryString.AllKeys)
                    {
                        string[] queryValues = e.Request.QueryString.GetValues(key);

                        // 引数が 1 津の場合でカンマ区切りの場合は、カンマを Split する。
                        if ((queryValues.Length == 1) && (queryValues.First().Contains(",") == true))
                        {
                            DynamicHelper.AddProperty(document, key, queryValues.First().Split(","));
                        }
                        else
                        {
                            DynamicHelper.AddProperty(document, key, queryValues);
                        }
                    }

                    reqBody = JsonSerializer.Serialize(document);
                }

                CommonApiArgs commonApiArgs =
                    new CommonApiArgs(e.Request.RequestTraceIdentifier, Enum.Parse<CommonApiMethods>(e.Request.HttpMethod), path, reqBody);

                OnRequest(commonApiArgs);

                if (commonApiArgs.Error == CommonApiArgs.Errors.None)
                {
                    e.Response.StatusCode = (int)HttpStatusCode.OK;

                    if (commonApiArgs.ResponseBody != null)
                    {
                        e.Response.ContentType = CONTENT_TYPE_JSON;
                        writer.BaseStream.Write(JsonSerializer.SerializeToUtf8Bytes(commonApiArgs.ResponseBody));
                    }
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
                    
                    e.Response.ContentType = CONTENT_TYPE_JSON;
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
                // API の実処理内で発生する例外は、ここで処理する。
                // 各 API の中で個別の例外を細かく拾う必要なない。
                try
                {
                    // 各 API の処理を呼び出す。
                    // 内部で対象判定、処理の呼び出しが行われる。
                    commonApiController.Proc(apiArgs);
                }
                catch (Exception ex)
                {
                    apiArgs.ResponseBody = null;
                    apiArgs.SetException(ex);
                }

                // 処理完了していたら、他のコントローラーのチェックは行わない。
                if (apiArgs.Handled == true)
                {
                    break;
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
