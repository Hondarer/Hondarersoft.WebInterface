﻿using Hondarersoft.Hosting;
using Hondarersoft.Utility;
using Hondarersoft.WebInterface.Controllers;
using Hondarersoft.WebInterface.Schemas;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
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

                _logger.LogInformation("RegistController REGIST: {0}", System.Text.Json.JsonSerializer.Serialize(new Dictionary<string, object>()
                {
                    { "commonApiController", commonApiController.GetType().FullName },
                    { "ApiPath",commonApiController.ApiPath },
                    { "MatchingMethod",commonApiController.MatchingMethod.ToString() }
                }));
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

            if (webInterfaceIdentify == null)
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

                    switch (request.Method)
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
                    if (httpResponse == null)
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

                    response.ResponseBody = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, object>>(result)["result"].ToString();
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
                    methodsName = methodsName[1..];
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

            if ((response.IsSuccess == true) && (response.ResponseBody != null))
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
            List<JsonElement> documents = new List<JsonElement>();
            WebSocketBase webSocketBase = sender as WebSocketBase;

            try
            {
                JsonElement recieveDocument = System.Text.Json.JsonSerializer.Deserialize<JsonElement>(e.Message);

                if (recieveDocument.ValueKind == JsonValueKind.Array)
                {
                    documents.AddRange(recieveDocument.EnumerateArray());
                }
                else
                {
                    documents.Add(recieveDocument);
                }
            }
            catch
            {
                // json でない
                object _response = new JsonRpcErrorResponse() { Id = null, Error = new Error() { Code = ErrorsToCode[CommonApiArgs.Errors.InternalError], Message = CommonApiArgs.Errors.InternalError.ToString() } };

                // このタイミングで例外が発生しうる。その場合は何もできないので、ここで握りつぶす。
                try
                {
                    await webSocketBase.SendJsonAsync(e.WebSocketIdentify, _response);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning("WebSocketService_WebSocketRecieveTextImpl Exception: {0}", ex.ToString());
                }
                return;
            }

            foreach (JsonElement document in documents)
            {
                object response;
                try
                {
                    // jsonrpc メンバーのチェック
                    if (document.TryGetProperty("jsonrpc", out JsonElement jsonrpcElement) == false)
                    {
                        // jsonrpc メンバーがない
                        // TODO: 詳細エラーを返すようにする
                        response = new JsonRpcErrorResponse() { Id = null, Error = new Error() { Code = ErrorsToCode[CommonApiArgs.Errors.InternalError], Message = CommonApiArgs.Errors.InternalError.ToString() } };

                        // このタイミングで例外が発生しうる。全体の try-catch で対応。
                        await webSocketBase.SendJsonAsync(e.WebSocketIdentify, response);
                        continue;
                    }
                    if ((jsonrpcElement.ValueKind != JsonValueKind.String) || (jsonrpcElement.GetString() != "2.0"))
                    {
                        // jsonrpc メンバーが不正
                        // TODO: 詳細エラーを返すようにする
                        response = new JsonRpcErrorResponse() { Id = null, Error = new Error() { Code = ErrorsToCode[CommonApiArgs.Errors.InternalError], Message = CommonApiArgs.Errors.InternalError.ToString() } };

                        // このタイミングで例外が発生しうる。全体の try-catch で対応。
                        await webSocketBase.SendJsonAsync(e.WebSocketIdentify, response);
                        continue;
                    }

                    // result か error があったら応答電文

                    bool isResult = document.TryGetProperty("result", out JsonElement resultElement);

                    bool isError = document.TryGetProperty("error", out JsonElement errorElement);

                    // id メンバーの取り出し
                    object id = null;
                    if (document.TryGetProperty("id", out JsonElement idElement) == true)
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
                            // レスポンスでここにたどり着くケースは、エラーを返すべきではない
                            if ((isResult != true) && (isError != true))
                            {
                                // TODO: 詳細エラーを返すようにする
                                response = new JsonRpcErrorResponse() { Id = null, Error = new Error() { Code = ErrorsToCode[CommonApiArgs.Errors.InternalError], Message = CommonApiArgs.Errors.InternalError.ToString() } };

                                // このタイミングで例外が発生しうる。全体の try-catch で対応。
                                await webSocketBase.SendJsonAsync(e.WebSocketIdentify, response);
                            }
                            continue;
                        }
                    }

                    if ((isResult == true) || (isError == true))
                    {
                        // 応答電文の処理

                        CommonApiResponse commonApiResponse = new CommonApiResponse();

                        if (isResult == true)
                        {
                            commonApiResponse.ResponseBody = resultElement.ToString();
                            commonApiResponse.IsSuccess = true;
                        }

                        if (isError == true)
                        {
                            try
                            {
                                // error: null の場合は例外処理にまかせる
                                commonApiResponse.Error = System.Text.Json.JsonSerializer.Deserialize<ErrorWithData>(errorElement.ToString());
                            }
                            catch
                            {
                                // Errorのパース失敗
                                // レスポンスに対して発生したエラーの返送は規約には規定がない。無限ループの危険性を排除するため NOP とする
                                // TODO: ログ
                                continue;
                            }
                        }

                        if (id == null)
                        {
                            // 相手方で id が特定できなかった場合のレスポンスやエラー通知
                            // 何かの処理を行うことはしない。エラーが相手で発生したことを記録する。
                            // TODO: ログ
                            continue;
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

                        continue;
                    }

                    // この段階でリクエスト電文なので method があるはず
                    if (document.TryGetProperty("method", out JsonElement methodElement) == false)
                    {
                        // リクエストでもレスポンスでもない
                        // TODO: 詳細エラーを返すようにする
                        response = new JsonRpcErrorResponse() { Id = id, Error = new Error() { Code = ErrorsToCode[CommonApiArgs.Errors.InternalError], Message = CommonApiArgs.Errors.InternalError.ToString() } };

                        // このタイミングで例外が発生しうる。全体の try-catch で対応。
                        await webSocketBase.SendJsonAsync(e.WebSocketIdentify, response);
                        continue;
                    }

                    // メソッド名の特定
                    // JSON-RPC メソッド名の末尾を HTTP メソッドとして取り出し、
                    // ドット結合をピリオド結合にし、ルート記号を付加する
                    string jsonrpcMethod = methodElement.GetString();
                    string upperJsonrpcMethod = jsonrpcMethod.ToUpper();
                    CommonApiMethods method = CommonApiMethods.UNKNOWN;
                    string path = string.Empty;
                    foreach (string methodEnum in Enum.GetNames(typeof(CommonApiMethods)))
                    {
                        if (upperJsonrpcMethod.EndsWith("." + methodEnum) == true)
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

                        // このタイミングで例外が発生しうる。全体の try-catch で対応。
                        await webSocketBase.SendJsonAsync(e.WebSocketIdentify, response);
                        continue;
                    }

                    object paramsValue = null;
                    if (document.TryGetProperty("params", out JsonElement paramsElement) == true)
                    {
                        paramsValue = paramsElement.ToString();
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
                            // 正常終了の場合は、返送不要。
                            continue;
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

                    // このタイミングで例外が発生しうる。全体の try-catch で対応。
                    await webSocketBase.SendJsonAsync(e.WebSocketIdentify, response);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning("WebSocketService_WebSocketRecieveTextImpl Exception: {0}", ex.ToString());
                }
            }
        }

        private void HttpService_HttpRequest(object sender, HttpRequestEventArgs e)
        {
            StreamReader reader = null;
            StreamWriter writer = null;

            try
            {
                e.HttpListenerContext.Response.ContentEncoding = Encoding.UTF8;

                reader = new StreamReader(e.HttpListenerContext.Request.InputStream);
                writer = new StreamWriter(e.HttpListenerContext.Response.OutputStream);
                string reqBody = reader.ReadToEnd();

                string responseContent;

                try
                {
                    // body が xml の場合、json に変換する。
                    if (string.IsNullOrEmpty(reqBody) != true)
                    {
                        if ((e.HttpListenerContext.Request.ContentType == "application/xml") || (e.HttpListenerContext.Request.ContentType == "text/xml"))
                        {
                            XDocument xDocument = XDocument.Parse(reqBody);

                            // omitRootObject を true にし、ルートオブジェクトを json に含めないようにしている。
                            // xml で要求が来た場合のルートオブジェクト名は必須ではあるが何でもよい。
                            reqBody = JsonConvert.SerializeXNode(xDocument.Root, Newtonsoft.Json.Formatting.None, true);
                        }
                    }

                    // クエリストリングを除いたパスの取得
                    string path = e.HttpListenerContext.Request.RawUrl.Split('?').First();

                    // GET で クエリストリングが存在する場合に、
                    // 内部処理では body に書かれたものとしてパラメーターを処理するため詰め替える。
                    // パラメーターはキー名がメンバ名となり、値は常に string[] となる。
                    if ((e.HttpListenerContext.Request.HttpMethod == "GET") && (e.HttpListenerContext.Request.QueryString.Count > 0))
                    {
                        Dictionary<string, List<string>> queryDictionary = new Dictionary<string, List<string>>();

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
                            if (queryDictionary.ContainsKey(_key) == false)
                            {
                                queryDictionary.Add(_key, new List<string>());
                            }
                            if ((queryValues.Length == 1) && (queryValues.First().Contains(",") == true))
                            {
                                queryDictionary[_key].AddRange(queryValues.First().Split(","));
                            }
                            else
                            {
                                queryDictionary[_key].AddRange(queryValues);
                            }
                        }

                        reqBody = System.Text.Json.JsonSerializer.Serialize(queryDictionary);
                    }

                    CommonApiArgs commonApiArgs =
                        new CommonApiArgs(e.HttpListenerContext.Request.RequestTraceIdentifier, Enum.Parse<CommonApiMethods>(e.HttpListenerContext.Request.HttpMethod), path, reqBody);

                    OnRequest(commonApiArgs);

                    if (commonApiArgs.Error == CommonApiArgs.Errors.None)
                    {
                        if (commonApiArgs.ResponseBody != null)
                        {
                            e.HttpListenerContext.Response.StatusCode = (int)HttpStatusCode.OK;

                            // Excel か、xml しか処理できない相手には XML を返す。(Excel からの要求は、AcceptType 未指定のため本判定が必要)
                            if (((e.HttpListenerContext.Request.UserAgent != null) && (e.HttpListenerContext.Request.UserAgent.StartsWith("Excel/") == true)) ||
                                ((e.HttpListenerContext.Request.AcceptTypes != null) && ((e.HttpListenerContext.Request.AcceptTypes.Length == 1) && ((e.HttpListenerContext.Request.AcceptTypes.First() == "text/xml") || (e.HttpListenerContext.Request.AcceptTypes.First() == "application/xml")))))
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
                            e.HttpListenerContext.Response.StatusCode = (int)HttpStatusCode.NoContent;

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

                        // Excel か、xml しか処理できない相手には XML を返す。(Excel からの要求は、AcceptType 未指定のため本判定が必要)
                        if (((e.HttpListenerContext.Request.UserAgent != null) && (e.HttpListenerContext.Request.UserAgent.StartsWith("Excel/") == true)) ||
                            ((e.HttpListenerContext.Request.AcceptTypes != null) && ((e.HttpListenerContext.Request.AcceptTypes.Length == 1) && ((e.HttpListenerContext.Request.AcceptTypes.First() == "text/xml") || (e.HttpListenerContext.Request.AcceptTypes.First() == "application/xml")))))
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
                }
                catch (Exception ex)
                {
                    e.HttpListenerContext.Response.StatusCode = (int)HttpStatusCode.BadRequest;

                    ErrorWithData error = new ErrorWithData() { Data = ex.ToString() };
                    error.Code = ErrorsToCode[CommonApiArgs.Errors.ParseError];
                    error.Message = CommonApiArgs.Errors.ParseError.ToString();

                    // Excel か、xml しか処理できない相手には XML を返す。(Excel からの要求は、AcceptType 未指定のため本判定が必要)
                    if (((e.HttpListenerContext.Request.UserAgent != null) && (e.HttpListenerContext.Request.UserAgent.StartsWith("Excel/") == true)) ||
                        ((e.HttpListenerContext.Request.AcceptTypes != null) && ((e.HttpListenerContext.Request.AcceptTypes.Length == 1) && ((e.HttpListenerContext.Request.AcceptTypes.First() == "text/xml") || (e.HttpListenerContext.Request.AcceptTypes.First() == "application/xml")))))
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
                        responseContent = System.Text.Json.JsonSerializer.Serialize(error);
                    }
                }

                _logger.LogInformation("Response: {0}, {1}, {2}", e.HttpListenerContext.Response.StatusCode, e.HttpListenerContext.Response.ContentType, responseContent);

                if (responseContent != null)
                {
                    writer.BaseStream.Write(Encoding.UTF8.GetBytes(responseContent));
                }
            }
            catch (Exception ex)
            {
                e.HttpListenerContext.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
                _logger.LogError(ex.ToString());
            }
            finally
            {
                try
                {
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
                catch
                {
                    // NOP
                }
            }
        }

        public virtual void OnRequest(CommonApiArgs apiArgs)
        {
            _logger.LogInformation("OnRequest START: {0}", System.Text.Json.JsonSerializer.Serialize(new Dictionary<string, object>()
            {
                { "Identifier",apiArgs.Identifier },
                { "Method",apiArgs.Method.ToString() },
                { "Path",apiArgs.Path },
                { "RegExMatchGroups",apiArgs.RegExMatchGroups },
                { "RequestBody",apiArgs.RequestBody }
            }));

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
                    _logger.LogInformation("OnRequest PROCESSED: {0}", ((object)commonApiController).GetType().FullName);
                    break;
                }
            }

            // どのコントローラーも処理しなかった場合は、エラーを返す。
            if (apiArgs.Handled == false)
            {
                apiArgs.SetError(CommonApiArgs.Errors.MethodNotFound, "Not eligible for processing for all registered controllers.");
            }

            _logger.LogInformation("OnRequest END: {0}", System.Text.Json.JsonSerializer.Serialize(new Dictionary<string, object>()
            {
                { "Handled",apiArgs.Handled },
                { "ResponseBody",apiArgs.ResponseBody },
                { "Error",apiArgs.Error.ToString() },
                { "ErrorMessage",apiArgs.ErrorMessage },
                { "ErrorDetails",apiArgs.ErrorDetails }
            }));
        }
    }
}
