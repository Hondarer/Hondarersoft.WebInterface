using Hondarersoft.Hosting;
using Hondarersoft.Utility;
using Hondarersoft.WebInterface.Controllers;
using Hondarersoft.WebInterface.Schemas;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Dynamic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace Hondarersoft.WebInterface
{
    public class CommonApiService : ICommonApiService // TODO: IDisposable 化
    {
        private const string CONTENT_TYPE_JSON = "application/json";
        private const string CONTENT_TYPE_XML = "text/xml";

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
            if (webInterfaceBase is IHttpService)
            {
                (webInterfaceBase as IHttpService).HttpRequest += HttpService_HttpRequest;
            }
            if (webInterfaceBase is IWebSocketBase)
            {
                (webInterfaceBase as IWebSocketBase).WebSocketTextRecieved += WebSocketService_WebSocketRecieveText;
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

            if (webInterface is IHttpClient)
            {
                HttpResponseMessage httpResponse = null;
                try
                {
                    StringContent content;
                    if (request.RequestBody != null)
                    {

                        content = new StringContent(System.Text.Json.JsonSerializer.Serialize(request.RequestBody), Encoding.UTF8, @"application/json");
                    }
                    else
                    {
                        content = null;
                    }

                    switch(request.Method)
                    {
                        case CommonApiMethods.GET:
                            // GET に content は指定不可
                            httpResponse = await (webInterface as IHttpClient).GetAsync(request.Path, request.Timeout);
                            break;
                        case CommonApiMethods.POST:
                            httpResponse = await (webInterface as IHttpClient).PostAsync(request.Path, content, request.Timeout);
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
                    ErrorWithData result;
                    try
                    {
                        result = await httpResponse.Content.ReadAsJsonAsync<ErrorWithData>();
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
                    response.ResponseBody = System.Text.Json.JsonSerializer.Deserialize<T>(response.ResponseBody.ToString());
                }
                catch
                {
                    // Result のデシリアライズに失敗
                    response.IsSuccess = false;
                }
            }

            return response;
        }

        private void WebSocketService_WebSocketRecieveText(object sender, WebSocketRecieveTextEventArgs e)
        {
            WebSocketService_WebSocketRecieveTextImpl(sender, e).NoWaitAndWatchException();
        }

        private async Task WebSocketService_WebSocketRecieveTextImpl(object sender, WebSocketRecieveTextEventArgs e)
        {
            // TODO: バッチ処理に対応していない(仕様にはあるが必要性は疑問)
            //       受信したデータがいきなり配列の場合はバッチ処理

            // メソッド全体(バッチ処理の場合はひとつひとつ)を try - catch する。

            WebSocketBase webSocketBase = sender as WebSocketBase;
            dynamic document;
            object response;

            try
            {
                document = System.Text.Json.JsonSerializer.Deserialize<ExpandoObject>(e.Message);
            }
            catch
            {
                // json でない
                response = new JsonRpcErrorResponse() { Id = null, Error = new Error() { Code = ErrorsToCode[CommonApiArgs.Errors.InternalError], Message = CommonApiArgs.Errors.InternalError.ToString() } };
                // TODO: このタイミングで例外が発生しうる。その場合は何もできないので、ここで握りつぶす。
                await webSocketBase.SendJsonAsync(e.WebSocketIdentify, response);
                return;
            }

            // jsonrpc メンバーのチェック
            if ((DynamicHelper.IsPropertyExist(document, "jsonrpc") == false) ||
                (document.jsonrpc.ToString() != "2.0"))
            {
                // jsonrpc メンバーがないか、バージョンが異なる
                // TODO: 詳細エラーを返すようにする
                response = new JsonRpcErrorResponse() { Id = null, Error = new Error() { Code = ErrorsToCode[CommonApiArgs.Errors.InternalError], Message = CommonApiArgs.Errors.InternalError.ToString() } };
                // TODO: このタイミングで例外が発生しうる。その場合は何もできないので、ここで握りつぶす。
                await webSocketBase.SendJsonAsync(e.WebSocketIdentify, response);
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
                    id = idElement.GetInt64(); // TODO: 念のため try - catch したほうがいい
                }
                else if (idElement.ValueKind == JsonValueKind.String)
                {
                    id = idElement.GetString();
                }
                else
                {
                    // id フィールドのフォーマット不良。
                    // TODO: レスポンスでここにたどり着くケースは、エラーを返すべきではないかも
                    // TODO: 詳細エラーを返すようにする
                    response = new JsonRpcErrorResponse() { Id = null, Error = new Error() { Code = ErrorsToCode[CommonApiArgs.Errors.InternalError], Message = CommonApiArgs.Errors.InternalError.ToString() } };
                    // TODO: このタイミングで例外が発生しうる。その場合は何もできないので、ここで握りつぶす。
                    await webSocketBase.SendJsonAsync(e.WebSocketIdentify, response);
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
                    object result = DynamicHelper.GetProperty(document, "result");

                    if (result != null)
                    {
                        commonApiResponse.ResponseBody = result.ToString();
                    }

                    commonApiResponse.IsSuccess = true;
                }

                if (DynamicHelper.IsPropertyExist(document, "error") == true)
                {
                    try
                    {
                        // error: null の場合は例外処理にまかせる
                        commonApiResponse.Error = System.Text.Json.JsonSerializer.Deserialize<ErrorWithData>(DynamicHelper.GetProperty(document, "error").ToString());
                    }
                    catch
                    {
                        // Errorのパース失敗
                        // レスポンスに対して発生したエラーの返送は規約には規定がない。無限ループの危険性を排除するため NOP とする
                        // TODO: ログ
                        return;
                    }
                }

                if (id == null)
                {
                    // 相手方で id が特定できなかった場合のレスポンスやエラー通知
                    // 何かの処理を行うことはしない。エラーが相手で発生したことを記録する。
                    // TODO: ログ
                    return;
                }

                // 待ち合わせ中の要求があったら、応答データを登録する
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

            // この段階でリクエスト電文なので method があるはず
            if (DynamicHelper.IsPropertyExist(document, "result") != true)
            {
                // リクエストでもレスポンスでもない
                // TODO: 詳細エラーを返すようにする
                response = new JsonRpcErrorResponse() { Id = id, Error = new Error() { Code = ErrorsToCode[CommonApiArgs.Errors.InternalError], Message = CommonApiArgs.Errors.InternalError.ToString() } };
                // TODO: このタイミングで例外が発生しうる。その場合は何もできないので、ここで握りつぶす。
                await webSocketBase.SendJsonAsync(e.WebSocketIdentify, response);
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
                // TODO: 詳細エラーを返すようにする
                response = new JsonRpcErrorResponse() { Id = id, Error = new Error() { Code = ErrorsToCode[CommonApiArgs.Errors.MethodNotFound], Message = CommonApiArgs.Errors.MethodNotFound.ToString() } };
                // TODO: このタイミングで例外が発生しうる。その場合は何もできないので、ここで握りつぶす。
                await webSocketBase.SendJsonAsync(e.WebSocketIdentify, response);
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

            if (apiArgs.Error == CommonApiArgs.Errors.None)
            {
                if (apiArgs.Identifier != null)
                {
                    // id が設定されている場合のレスポンス
                    response = new JsonRpcNormalResponse() { Id = apiArgs.Identifier, Result = apiArgs.ResponseBody };
                }
                else
                {
                    // id が設定されていない場合は、notify。
                    // 正常終了の場合は、返送不要。エラーの場合は id = null でエラーを返す。
                    return;
                }
            }
            else
            {
                int code = ErrorsToCode[CommonApiArgs.Errors.InternalError];
                if (ErrorsToCode.ContainsKey(apiArgs.Error) == true)
                {
                    code = ErrorsToCode[apiArgs.Error];
                }

                Error error;
                if (apiArgs.ErrorDetails != null)
                {
                    error = new ErrorWithData() { Data = apiArgs.ErrorDetails };
                }
                else
                {
                    error = new Error();
                }
                error.Code = code;
                error.Message = apiArgs.ErrorMessage;

                if (apiArgs.ErrorDetails != null)
                {
                    response = new JsonRpcErrorResponseWithData() { Id = apiArgs.Identifier, Error = error as ErrorWithData };
                }
                else
                {
                    response = new JsonRpcErrorResponse() { Id = apiArgs.Identifier, Error = error };
                }
            }

            // TODO: このタイミングで例外が発生しうる。その場合は何もできないので、ここで握りつぶす。
            await webSocketBase.SendJsonAsync(e.WebSocketIdentify, response);
        }

        private void HttpService_HttpRequest(object sender, HttpRequestEventArgs e)
        {
            StreamReader reader = null;
            StreamWriter writer = null;

            // TODO: User-Agent が "Excel/" で始まっている場合は Excel。
            // Excel の場合は、後続の処理を考慮すると、XML で返したほうが優しい。
            // Accept type は Excel からの場合、null だった。

            try
            {
                e.HttpListenerContext.Response.ContentEncoding = Encoding.UTF8;

                reader = new StreamReader(e.HttpListenerContext.Request.InputStream);
                writer = new StreamWriter(e.HttpListenerContext.Response.OutputStream);
                string reqBody = reader.ReadToEnd();

                // クエリストリングを除いたパスの取得
                string path = e.HttpListenerContext.Request.RawUrl.Split('?').First();

                // GET で クエリストリングが存在する場合に、
                // 内部処理では body に書かれたものとしてパラメーターを処理するため詰め替える。
                // パラメーターはキー名がメンバ名となり、値は常に string[] となる。
                if ((e.HttpListenerContext.Request.HttpMethod == "GET") && (e.HttpListenerContext.Request.QueryString.Count > 0))
                {
                    dynamic document = new ExpandoObject();

                    foreach (string key in e.HttpListenerContext.Request.QueryString.AllKeys)
                    {
                        string[] queryValues = e.HttpListenerContext.Request.QueryString.GetValues(key);

                        string _key;
                        if (key == null)
                        {
                            // キーなしは null キーになるが、json で表せないのでキーを与える。
                            _key = "_defaultKey";
                        }
                        else if (key == "_defaultKey")
                        {
                            // 上記の処理との都合で、要求自体に "_defaultKey" キーが含まれている場合は、
                            // キー重複になってしなうので、当該キーの処理は行わない。
                            // API 仕様策定時、"_defaultKey" キーを規定しないこと。
                            continue;
                        }
                        else
                        {
                            _key = key;
                        }

                        // 引数が 1 つの場合でカンマ区切りの場合は、カンマを Split する。
                        if ((queryValues.Length == 1) && (queryValues.First().Contains(",") == true))
                        {
                            DynamicHelper.AddProperty(document, _key, queryValues.First().Split(","));
                        }
                        else
                        {
                            DynamicHelper.AddProperty(document, _key, queryValues);
                        }
                    }

                    reqBody = System.Text.Json.JsonSerializer.Serialize(document);
                }

                CommonApiArgs commonApiArgs =
                    new CommonApiArgs(e.HttpListenerContext.Request.RequestTraceIdentifier, Enum.Parse<CommonApiMethods>(e.HttpListenerContext.Request.HttpMethod), path, reqBody);

                _logger.LogInformation("OnRequest: apiArgs: {0}", commonApiArgs);

                OnRequest(commonApiArgs);

                string responseContent;
                if (commonApiArgs.Error == CommonApiArgs.Errors.None)
                {
                    e.HttpListenerContext.Response.StatusCode = (int)HttpStatusCode.OK;

                    if (commonApiArgs.ResponseBody != null)
                    {
                        // Excel か、text/xml しか処理できない相手には XML を返す。
                        // TODO: Content を受けるときもこの判断必要。また、判定は、json が処理できず、text/xml または application/xml が処理できる場合としたほうがいい。
                        if ((e.HttpListenerContext.Request.UserAgent.StartsWith("Excel/") == true) ||
                            ((e.HttpListenerContext.Request.AcceptTypes.Length == 1) && (e.HttpListenerContext.Request.AcceptTypes.First() == "text/xml")))
                        {
                            // xml
                            e.HttpListenerContext.Response.ContentType = CONTENT_TYPE_XML;
                            XDocument xDocument;
                            xDocument = JsonConvert.DeserializeXNode(System.Text.Json.JsonSerializer.Serialize(new RestNormalResponse() { Result = commonApiArgs.ResponseBody }), "results");
                            responseContent = xDocument.ToString();
                        }
                        else
                        {
                            // json
                            e.HttpListenerContext.Response.ContentType = CONTENT_TYPE_JSON;
                            responseContent = System.Text.Json.JsonSerializer.Serialize(new RestNormalResponse() { Result = commonApiArgs.ResponseBody });
                        }
                    }
                    else
                    {
                        responseContent = null;
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
                            e.HttpListenerContext.Response.StatusCode = (int)HttpStatusCode.BadRequest;
                            break;
                        case CommonApiArgs.Errors.MethodNotFound:
                            e.HttpListenerContext.Response.StatusCode = (int)HttpStatusCode.NotFound;
                            break;
                        case CommonApiArgs.Errors.InternalError:
                            e.HttpListenerContext.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
                            break;
                        case CommonApiArgs.Errors.MethodNotAvailable:
                            e.HttpListenerContext.Response.StatusCode = (int)HttpStatusCode.MethodNotAllowed;
                            break;
                        default:
                            // 下記は念のため(本来通過することはない)
                            e.HttpListenerContext.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
                            break;
                    }

                    Error error;
                    if (commonApiArgs.ErrorDetails != null)
                    {
                        error = new ErrorWithData() { Data = commonApiArgs.ErrorDetails };
                    }
                    else
                    {
                        error = new Error();
                    }
                    error.Code = code;
                    error.Message = commonApiArgs.ErrorMessage;

                    // Excel か、text/xml しか処理できない相手には XML を返す。
                    // TODO: Content を受けるときもこの判断必要。また、判定は、json が処理できず、text/xml または application/xml が処理できる場合としたほうがいい。
                    if (((e.HttpListenerContext.Request.UserAgent != null) && (e.HttpListenerContext.Request.UserAgent.StartsWith("Excel/") == true)) ||
                        ((e.HttpListenerContext.Request.AcceptTypes.Length == 1) && (e.HttpListenerContext.Request.AcceptTypes.First() == "text/xml")))
                    {
                        // xml
                        RestErrorResponse restErrorResponse = new RestErrorResponse() { Error = error };
                        e.HttpListenerContext.Response.ContentType = CONTENT_TYPE_XML;
                        XDocument xDocument = JsonConvert.DeserializeXNode(System.Text.Json.JsonSerializer.Serialize(restErrorResponse));
                        responseContent = xDocument.ToString();
                    }
                    else
                    {
                        // json
                        e.HttpListenerContext.Response.ContentType = CONTENT_TYPE_JSON;
                        if (commonApiArgs.ErrorDetails != null)
                        {
                            responseContent = System.Text.Json.JsonSerializer.Serialize(error as ErrorWithData);
                        }
                        else
                        {
                            responseContent = System.Text.Json.JsonSerializer.Serialize(error);
                        }
                    }
                }

                _logger.LogInformation("Response: {0}, {1}, {2}", e.HttpListenerContext.Response.StatusCode, e.HttpListenerContext.Response.ContentType, responseContent);
                writer.BaseStream.Write(Encoding.UTF8.GetBytes(responseContent));
            }
            catch (Exception ex)
            {
                //resBoby = CreateErrorResponse(ErrorCode.SYSTEM_ERROR, String.Format(Resources.ErrorUnexpected, ex.Message));
                e.HttpListenerContext.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
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
