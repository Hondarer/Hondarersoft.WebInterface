// https://github.com/yunbow/CSharp-WebAPI

using Hondarersoft.Utility;
using Hondarersoft.Utility.Extensions;
using Microsoft.Extensions.Configuration;
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

        public new IWebApiService LoadConfiguration(IConfiguration configurationRoot)
        {
            WebApiServiceConfigEntry webApiServiceConfig = configurationRoot.Get<WebApiServiceConfigEntry>();

            if (webApiServiceConfig.AllowCORS != null)
            {
                AllowCORS = (bool)webApiServiceConfig.AllowCORS;
            }

            return base.LoadConfiguration(webApiServiceConfig) as IWebApiService;
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

            ProcessHttpRequest(_listener).NoWaitAndWatchException();

            return Task.CompletedTask;
        }

        protected async Task ProcessHttpRequest(HttpListener httpListener)
        {
            try
            {
                while (httpListener.IsListening == true)
                {
                    // 接続待機
                    HttpListenerContext httpListenerContext = await httpListener.GetContextAsync();

                    if (httpListener.IsListening == false)
                    {
                        break;
                    }

                    if (AllowCORS == true)
                    {
                        if (httpListenerContext.Request.HttpMethod == "OPTIONS")
                        {
                            httpListenerContext.Response.AddHeader("Access-Control-Allow-Headers", "Content-Type, Accept, X-Requested-With");
                            httpListenerContext.Response.AddHeader("Access-Control-Allow-Methods", "GET, POST, PUT, DELETE");
                            httpListenerContext.Response.AddHeader("Access-Control-Max-Age", "1728000");
                        }
                        httpListenerContext.Response.AppendHeader("Access-Control-Allow-Origin", "*");
                    }

                    try
                    {
                        await Invoke(httpListenerContext);
                    }
                    catch
                    {
                        // レスポンスにある程度値がセットされている場合、このタイミングで 500 にすることができない。
                        // NOP
                    }
                    finally
                    {
                        _logger.LogInformation("Response: {0} {1} {2}", httpListenerContext.Request.RequestTraceIdentifier.ToString(), httpListenerContext.Response.StatusCode, httpListenerContext.Response.StatusDescription);

                        if (httpListenerContext.Response != null)
                        {
                            httpListenerContext.Response.Close();
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError("Exception at ProcessHttpRequest method.\r\n{0}", ex.ToString());

                Stop();
            }
        }

        protected virtual async Task Invoke(HttpListenerContext httpListenerContext)
        {
            _logger.LogInformation("Request: {0} {1} {2}", httpListenerContext.Request.RequestTraceIdentifier.ToString(), httpListenerContext.Request.HttpMethod, httpListenerContext.Request.RawUrl);

            if (WebApiRequest != null)
            {
                WebApiRequest(this, new IWebApiService.WebApiRequestEventArgs(httpListenerContext));
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
                _listener.Stop();
                _listener.Close();

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
