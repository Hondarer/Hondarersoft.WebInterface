// https://github.com/yunbow/CSharp-WebAPI

using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Hondarersoft.WebInterface
{
    public class WebApiService : WebInterface, IWebApiService, IWebInterfaceService
    {
        public event IWebApiService.WebApiRequestHandler WebApiRequest;

        //private static Logger log = Logger.GetInstance();
        private HttpListener _listener = null;

        public bool AllowCORS { get; set; } = false;

        public WebApiService(ILogger<WebApiService> logger) : base(logger)
        {
            Hostname = "+";
        }

        /// <summary>
        /// APIサービスを起動する
        /// </summary>
        public Task StartAsync()
        {
            if ((string.IsNullOrEmpty(Hostname) == true) ||
                (PortNumber == 0))
            {
                throw new Exception("invalid endpoint parameter");
            }

            // HTTPサーバーを起動する
            _listener = new HttpListener();

            string ssl = string.Empty;
            if (UseSSL == true)
            {
                ssl = "s";
            }

            string tail = string.Empty;
            if (string.IsNullOrEmpty(BasePath) != true)
            {
                tail = "/";
            }

            _listener.Prefixes.Add($"http{ssl}://{Hostname}:{PortNumber}/{BasePath}{tail}");
            _listener.Start();

            ProcessHttpRequest(_listener);

            return Task.CompletedTask;
        }

        protected async void ProcessHttpRequest(HttpListener httpListener)
        {
            while (httpListener.IsListening == true)
            {
                // 接続待機
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
                    _logger.LogInformation("Request: {0} {1} {2}", req.RequestTraceIdentifier.ToString(), req.HttpMethod, req.RawUrl);

                    // TODO: 例外を処理したほうがいい
                    if (WebApiRequest != null)
                    {
                        WebApiRequest(this, new IWebApiService.WebApiRequestEventArgs(req, res));
                    }

                    _logger.LogInformation("Response: {0} {1} {2}", req.RequestTraceIdentifier.ToString(), res.StatusCode, res.StatusDescription);
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
                this._listener.Stop();
                this._listener.Close();

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
