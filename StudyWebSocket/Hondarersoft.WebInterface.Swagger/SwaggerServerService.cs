using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Hondarersoft.WebInterface.Swagger
{
    public class SwaggerServerService : WebApiService, ISwaggerServerService
    {
        public Func<Stream> SwaggerYamlResolver { get; set; }

        public SwaggerServerService(ILogger<SwaggerServerService> logger) : base(logger)
        {
        }

        public ISwaggerServerService SetSwaggerYamlResolver(Func<Stream> swaggerYamlResolver)
        {
            SwaggerYamlResolver = swaggerYamlResolver;
            return this;
        }

        public ISwaggerServerService LoadConfiguration(IConfiguration configurationRoot)
        {
            return base.LoadConfiguration(configurationRoot) as ISwaggerServerService;
        }

        protected override async Task Invoke(HttpListenerContext httpListenerContext)
        {
            // reference embedded resouces
            const string prefix = "Hondarersoft.WebInterface.Swagger.SwaggerUI.";

            var path = httpListenerContext.Request.RawUrl.Substring(BasePath.Length+1);
            if(string.IsNullOrEmpty(path)==true)
            {
                httpListenerContext.Response.StatusCode = (int)HttpStatusCode.NotFound;
                return;
            }
            if (path == "/") path = "index.html";
            path = path.Trim('/');
            var filePath = prefix + path.Replace("/", ".");
            var mediaType = GetMediaType(filePath);

            if (path == "swagger.yaml")
            {
                if (SwaggerYamlResolver == null)
                {
                    httpListenerContext.Response.StatusCode = (int)HttpStatusCode.NotFound;
                    return;
                }

                using (var stream = SwaggerYamlResolver())
                {
                    if (stream == null)
                    {
                        httpListenerContext.Response.StatusCode = (int)HttpStatusCode.NotFound;
                        return;
                    }

                    httpListenerContext.Response.ContentType = mediaType;
                    httpListenerContext.Response.StatusCode = (int)HttpStatusCode.OK;

                    var writer = new StreamWriter(httpListenerContext.Response.OutputStream);
                    await stream.CopyToAsync(writer.BaseStream);
                }

                return;
            }

            var myAssembly = typeof(SwaggerServerService).GetTypeInfo().Assembly;

            using (var stream = myAssembly.GetManifestResourceStream(filePath))
            {
                if (stream == null)
                {
                    httpListenerContext.Response.StatusCode = (int)HttpStatusCode.NotFound;
                    return;
                }

                httpListenerContext.Response.ContentType = mediaType;
                httpListenerContext.Response.StatusCode = (int)HttpStatusCode.OK;

                var writer = new StreamWriter(httpListenerContext.Response.OutputStream);
                await stream.CopyToAsync(writer.BaseStream);
            }

            await base.Invoke(httpListenerContext);
        }

        static string GetMediaType(string path)
        {
            var extension = path.Split('.').Last();

            switch (extension)
            {
                case "css":
                    return "text/css";
                case "js":
                    return "text/javascript";
                case "json":
                    return "application/json";
                case "gif":
                    return "image/gif";
                case "png":
                    return "image/png";
                case "eot":
                    return "application/vnd.ms-fontobject";
                case "woff":
                    return "application/font-woff";
                case "woff2":
                    return "application/font-woff2";
                case "otf":
                    return "application/font-sfnt";
                case "ttf":
                    return "application/font-sfnt";
                case "svg":
                    return "image/svg+xml";
                case "ico":
                    return "image/x-icon";
                default:
                    return "text/html";
            }
        }
    }
}
