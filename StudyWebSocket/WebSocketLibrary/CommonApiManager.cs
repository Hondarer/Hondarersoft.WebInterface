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
using WebSocketLibrary.Schemas;

namespace WebSocketLibrary
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
            //メッセージをデシリアライズ

            // TODO: まだ未実装

            // 最初に通知、リクエスト、レスポンス、エラーを判断する

            // 通知とリクエストを処理する
            // レスポンスとエラーはリクエストに紐づく応答として処理する

            // メソッドの分解ができてない

            WebSocketBase webSocketBase = sender as WebSocketBase;

            dynamic document = JsonSerializer.Deserialize<ExpandoObject>(e.Message);

            CommonApiArgs commonApiArgs =
                new CommonApiArgs(CommonApiArgs.Methods.GET, document.method.ToString(), DynamicHelper.GetProperty(document, "params").ToString());

            OnRequest(commonApiArgs);

            object response;

            if (commonApiArgs.Error == CommonApiArgs.Errors.None)
            {
                response = new JsonRpcNormalResponse() { Data = JsonSerializer.Serialize(commonApiArgs.ResponseBody) };
            }
            else
            {
                int code = ErrorsToCode[CommonApiArgs.Errors.InternalError];
                if (ErrorsToCode.ContainsKey(commonApiArgs.Error) == true)
                {
                    code = ErrorsToCode[commonApiArgs.Error];
                }

                response = new JsonRpcErrorResponse() { Error = new Error() { Code = code, Message = commonApiArgs.ErrorMessage } };
            }

            await webSocketBase.SendJsonAsync(response, e.WebSocket);
        }

        /// <summary>
        /// APIパスを取得する
        /// </summary>
        /// <param name="srcPath">URLパス</param>
        /// <returns>APIパス</returns>
        private string GetApiPath(string srcPath)
        {
            string[] path = srcPath.Split('?');
            string condition = String.Format(@"^/{0}", "Temporary_Listen_Addresses");
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
                    new CommonApiArgs(Enum.Parse<CommonApiArgs.Methods>(e.Request.HttpMethod), path, reqBody);

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
            //catch (ApiException ex)
            //{
            //    // APIエラー
            //    resBoby = CreateApiErrorResponse(ex);
            //}
            //catch (JsonReaderException ex)
            //{
            //    // JSON構文エラー
            //    resBoby = CreateErrorResponse(ErrorCode.ERROR_JSON_SYNTAX, String.Format(Resources.ErrorJsonSyntax, ex.Message));
            //    res.StatusCode = (int)HttpStatusCode.InternalServerError;
            //}
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

        public virtual void OnRequest(CommonApiArgs commonApiArgs)
        {
            // TODO: この部分は基本の通信処理と分けて考えるべき

            // ★★★ テスト ★★★
            if (commonApiArgs.Path.Equals("/cpumodes") && commonApiArgs.Method == CommonApiArgs.Methods.GET)
            {
                commonApiArgs.ResponseBody = new CpuModes() { new CpuMode() { Hostname = "localhoost" }, new CpuMode() { Hostname = "hostname2" } };
            }
            else if (commonApiArgs.Path.Equals("/cpumodes/localhost") && commonApiArgs.Method == CommonApiArgs.Methods.GET)
            {
                commonApiArgs.ResponseBody = new CpuMode() { Hostname = "localhoost" };
            }
            else
            {
                commonApiArgs.SetError(CommonApiArgs.Errors.MethodNotFound);
            }
        }

    }
}
