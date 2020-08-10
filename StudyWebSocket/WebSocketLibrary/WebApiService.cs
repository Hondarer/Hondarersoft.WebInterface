// https://github.com/yunbow/CSharp-WebAPI

using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using WebSocketLibrary.Schemas;

namespace WebSocketLibrary
{
    public class WebApiService : WebInterfaceBase
    {
        private const string CONTENT_TYPE_JSON = "application/json";

        //private static Logger log = Logger.GetInstance();
        private HttpListener listener;
        private WebApiControllerMapper mapper = new WebApiControllerMapper();

        public bool AllowCORS { get; set; } = false;

        /// <summary>
        /// APIサービスを起動する
        /// </summary>
        public void Start()
        {
            Assembly asm = Assembly.GetExecutingAssembly();
            string strSystemName = asm.GetName().Name;

            //log.Info("########## HTTP Server [start] ##########");
            //log.Info(">> System Name: " + strSystemName);
            //log.Info(">> System Version: " + asm.GetName().Version);

            try
            {
                // HTTPサーバーを起動する
                this.listener = new HttpListener();
                //this.listener.Prefixes.Add(String.Format("http://+:{0}/{1}/", Settings.Default.API_PORT, Settings.Default.API_PATH));
                this.listener.Prefixes.Add(String.Format("http://+:{0}/{1}/", 80, "Temporary_Listen_Addresses"));
                this.listener.Start();

                //log.Info(Resources.StartServer);
                //EventLog.WriteEntry(GetSystemName(), Resources.StartServer, EventLogEntryType.Information, (int)ErrorCode.SUCCESS);

                while (this.listener.IsListening)
                {
                    IAsyncResult result = this.listener.BeginGetContext(OnRequested, this.listener);
                    result.AsyncWaitHandle.WaitOne();
                }
            }
            catch (Exception ex)
            {
                //log.Error(ex.ToString());
                //EventLog.WriteEntry(GetSystemName(), ex.ToString(), EventLogEntryType.Error, (int)ErrorCode.ERROR_START);
            }
        }

        /// <summary>
        /// APIサービスを停止する
        /// </summary>
        public void Stop()
        {
            try
            {
                // HTTPサーバーを停止する
                this.listener.Stop();
                this.listener.Close();

                //log.Info(Resources.StopServer);
                //EventLog.WriteEntry(GetSystemName(), Resources.StopServer, EventLogEntryType.Information, (int)ErrorCode.SUCCESS);
            }
            catch (Exception ex)
            {
                //log.Error(ex.ToString());

                Assembly clsAsm = Assembly.GetExecutingAssembly();
                string strSystemName = clsAsm.GetName().Name;

                //EventLog.WriteEntry(GetSystemName(), ex.ToString(), EventLogEntryType.Error, (int)ErrorCode.ERROR_STOP);
            }

            //log.Info("########## HTTP Server [end] ##########");
        }

        /// <summary>
        /// リクエスト時の処理を実行する
        /// </summary>
        /// <param name="result">結果</param>
        private void OnRequested(IAsyncResult result)
        {
            HttpListener clsListener = (HttpListener)result.AsyncState;
            if (!clsListener.IsListening)
            {
                return;
            }

            HttpListenerContext context = clsListener.EndGetContext(result);
            HttpListenerRequest req = context.Request;
            HttpListenerResponse res = context.Response;

            if (AllowCORS == true)
            {
                if (req.HttpMethod == "OPTIONS")
                {
                    res.AddHeader("Access-Control-Allow-Headers", "Content-Type, Accept, X-Requested-With");
                    res.AddHeader("Access-Control-Allow-Methods", "GET, POST, PUT, DELETE");
                    res.AddHeader("Access-Control-Max-Age", "1728000");
                }
                res.AppendHeader("Access-Control-Allow-Origin", "*");
            }

            // TODO: req, resの基本イベントを作る

            StreamReader reader = null;
            StreamWriter writer = null;

            try
            {
                res.ContentType = CONTENT_TYPE_JSON;
                res.ContentEncoding = Encoding.UTF8;

                reader = new StreamReader(req.InputStream);
                writer = new StreamWriter(res.OutputStream);
                string reqBody = reader.ReadToEnd();

                string path = GetApiPath(req.RawUrl);

                //Console.WriteLine(req.QueryString.Get("hhh"));

                CommonApiArgs commonApiArgs =
                    new CommonApiArgs(Enum.Parse<CommonApiArgs.Methods>(req.HttpMethod), path, reqBody);

                OnRequest(commonApiArgs);

                if (commonApiArgs.Error == CommonApiArgs.Errors.None)
                {
                    res.StatusCode = (int)HttpStatusCode.OK;
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
                            res.StatusCode = (int)HttpStatusCode.BadRequest;
                            break;
                        case CommonApiArgs.Errors.MethodNotFound:
                            res.StatusCode = (int)HttpStatusCode.NotFound;
                            break;
                        case CommonApiArgs.Errors.InternalError:
                            res.StatusCode = (int)HttpStatusCode.InternalServerError;
                            break;
                        case CommonApiArgs.Errors.MethodNotAvailable:
                            res.StatusCode = (int)HttpStatusCode.MethodNotAllowed;
                            break;
                        default:
                            // 下記は念のため(本来通過することはない)
                            res.StatusCode = (int)HttpStatusCode.InternalServerError;
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
                res.StatusCode = (int)HttpStatusCode.InternalServerError;
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

                    if (null != res)
                    {
                        res.Close();
                    }
                }
                catch (Exception clsEx)
                {
                    //log.Error(clsEx.ToString());
                }
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
            string condition = String.Format(@"^/{0}", "Temporary_Listen_Addresses");
            //string condition = String.Format(@"^/{0}", Settings.Default.API_PATH);
            return Regex.Replace(path[0], condition, "");
        }
    }
}
