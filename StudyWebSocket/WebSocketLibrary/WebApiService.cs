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
        public class WebApiRequestEventArgs: EventArgs
        {
            public HttpListenerRequest Request { get; }
            public HttpListenerResponse Response { get; }

            public WebApiRequestEventArgs(HttpListenerRequest request, HttpListenerResponse response)
            {
                Request = request;
                Response = response;
            }
        }

        public delegate void WebApiRequestHandler(object sender, WebApiRequestEventArgs e);
        public event WebApiRequestHandler WebApiRequest;

        //private static Logger log = Logger.GetInstance();
        private HttpListener listener;

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
                this.listener.Prefixes.Add(String.Format("http://+:{0}/{1}/", 80, "Temporary_Listen_Addresses/v1.0"));
                this.listener.Start();

                //log.Info(Resources.StartServer);
                //EventLog.WriteEntry(GetSystemName(), Resources.StartServer, EventLogEntryType.Information, (int)ErrorCode.SUCCESS);

                ProcessHttpRequest(this.listener);

                //while (this.listener.IsListening)
                //{
                //    IAsyncResult result = this.listener.BeginGetContext(OnRequested, this.listener);
                //    result.AsyncWaitHandle.WaitOne();
                //}
            }
            catch (Exception ex)
            {
                //log.Error(ex.ToString());
                //EventLog.WriteEntry(GetSystemName(), ex.ToString(), EventLogEntryType.Error, (int)ErrorCode.ERROR_START);
            }
        }

        protected async void ProcessHttpRequest(HttpListener httpListener)
        {
            while (httpListener.IsListening == true)
            {
                /// 接続待機
                HttpListenerContext context = await httpListener.GetContextAsync();

                if (httpListener.IsListening == false)
                {
                    break;
                }

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

                try
                {
                    // TODO: 例外を処理したほうがいい
                    if (WebApiRequest != null)
                    {
                        WebApiRequest(this, new WebApiRequestEventArgs(req, res));
                    }
                }
                finally
                {
                    if (res != null)
                    {
                        res.Close();
                    }
                }
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

                //Assembly clsAsm = Assembly.GetExecutingAssembly();
                //string strSystemName = clsAsm.GetName().Name;

                //EventLog.WriteEntry(GetSystemName(), ex.ToString(), EventLogEntryType.Error, (int)ErrorCode.ERROR_STOP);
            }

            //log.Info("########## HTTP Server [end] ##########");
        }
    }
}
