// https://github.com/Cysharp/ConsoleAppFramework の
// https://github.com/Cysharp/ConsoleAppFramework/tree/master/src/ConsoleAppFramework.WebHosting を参考に実装。

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Threading.Tasks;

namespace Hondarersoft.WebInterface.Swagger
{
    /// <summary>
    /// Swagger UI を HTTP で提供するサービスを提供します。
    /// </summary>
    public class SwaggerServerService : HttpService, ISwaggerServerService
    {
        /// <summary>
        /// Swagger UI に表示する <see cref="Stream"/> を返す <see cref="Func{Stream}"/> を取得または設定します。
        /// </summary>
        public Func<Stream> SwaggerYamlResolver { get; set; } = null;

        /// <summary>
        /// <see cref="SwaggerServerService"/> の新しいインスタンスを生成します。
        /// </summary>
        /// <param name="logger">A generic interface for logging.</param>
        public SwaggerServerService(ILogger<SwaggerServerService> logger) : base(logger)
        {
        }

        /// <summary>
        /// Swagger UI に表示する <see cref="Stream"/> を返す <see cref="Func{Stream}"/> を設定します。
        /// </summary>
        /// <param name="swaggerYamlResolver">Swagger に表示する <see cref="Stream"/> を返す <see cref="Func{Stream}"/>。</param>
        /// <returns>この <see cref="ISwaggerServerService"/> のインスタンス。</returns>
        public ISwaggerServerService SetSwaggerYamlResolver(Func<Stream> swaggerYamlResolver)
        {
            SwaggerYamlResolver = swaggerYamlResolver;
            return this;
        }

        /// <summary>
        /// 設定を読み込みます。
        /// </summary>
        /// <param name="configurationRoot">読み込みの基準となる <see cref="IConfiguration"/>。</param>
        /// <returns>この <see cref="ISwaggerServerService"/> のインスタンス。</returns>
        public new ISwaggerServerService LoadConfiguration(IConfiguration configurationRoot)
        {
            return base.LoadConfiguration(configurationRoot) as ISwaggerServerService;
        }

        /// <summary>
        /// HTTP リクエストを処理します。
        /// </summary>
        /// <param name="httpListenerContext">処理対象の <see cref="HttpListenerContext"/>。</param>
        /// <returns>処理完了を待ち合わせる <see cref="Task"/>。</returns>
        protected override async Task Invoke(HttpListenerContext httpListenerContext)
        {
            // reference embedded resouces
            const string prefix = "Hondarersoft.WebInterface.Swagger.SwaggerUI.";

            string path = httpListenerContext.Request.RawUrl.Substring(BasePath.Length + 1);
            if (string.IsNullOrEmpty(path) == true)
            {
                httpListenerContext.Response.Redirect(httpListenerContext.Request.RawUrl + "/");
                return;
            }
            if (path == "/")
            {
                path = "index.html";
            }
            path = path.Trim('/');
            string filePath = prefix + path.Replace("/", ".");
            string mediaType = GetContentType(filePath);

            // swagger.yaml の中身は、このクラスの外から与えられる。
            if (path == "swagger.yaml")
            {
                // SwaggerYamlResolver が未設定。
                if (SwaggerYamlResolver == null)
                {
                    httpListenerContext.Response.StatusCode = (int)HttpStatusCode.NotFound;
                    return;
                }

                using (Stream stream = SwaggerYamlResolver())
                {
                    // Stream が null。
                    if (stream == null)
                    {
                        httpListenerContext.Response.StatusCode = (int)HttpStatusCode.NotFound;
                        return;
                    }

                    // Stream の中身を返す。
                    httpListenerContext.Response.ContentType = mediaType;
                    httpListenerContext.Response.StatusCode = (int)HttpStatusCode.OK;

                    StreamWriter writer = new StreamWriter(httpListenerContext.Response.OutputStream);
                    await stream.CopyToAsync(writer.BaseStream);
                }
                return;
            }

            // このアセンブリにあるリソースを返すため、Assembly を取得。
            Assembly myAssembly = typeof(SwaggerServerService).GetTypeInfo().Assembly;

            // 埋め込みリソースを返す。
            using (Stream stream = myAssembly.GetManifestResourceStream(filePath))
            {
                if (stream == null)
                {
                    httpListenerContext.Response.StatusCode = (int)HttpStatusCode.NotFound;
                    return;
                }

                httpListenerContext.Response.ContentType = mediaType;
                httpListenerContext.Response.StatusCode = (int)HttpStatusCode.OK;

                StreamWriter writer = new StreamWriter(httpListenerContext.Response.OutputStream);
                await stream.CopyToAsync(writer.BaseStream);
            }

            await base.Invoke(httpListenerContext);
        }

        /// <summary>
        /// ファイル名に対応するコンテント タイプを返します。
        /// </summary>
        /// <param name="path">ファイル名。</param>
        /// <returns>ファイル名に対応するコンテント タイプ。</returns>
        protected static string GetContentType(string path)
        {
            // 拡張子部分を取り出す。
            string extension = path.Split('.').Last();

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
                    // 特定できない場合は、text/html としておく。
                    return "text/html";
            }
        }
    }
}
