using Microsoft.Extensions.Configuration;
using System;
using System.IO;

namespace Hondarersoft.WebInterface.Swagger
{
    /// <summary>
    /// Swagger UI を HTTP で提供するサービスを提供します。
    /// </summary>
    public interface ISwaggerServerService : IHttpService
    {
        /// <summary>
        /// Swagger UI に表示する <see cref="Stream"/> を返す <see cref="Func{Stream}"/> を取得または設定します。
        /// </summary>
        public Func<Stream> SwaggerYamlResolver { get; set; }

        /// <summary>
        /// Swagger UI に表示する <see cref="Stream"/> を返す <see cref="Func{Stream}"/> を設定します。
        /// </summary>
        /// <param name="swaggerYamlResolver">Swagger に表示する <see cref="Stream"/> を返す <see cref="Func{Stream}"/>。</param>
        /// <returns>この <see cref="ISwaggerServerService"/> のインスタンス。</returns>
        public ISwaggerServerService SetSwaggerYamlResolver(Func<Stream> swaggerYamlResolver);

        /// <summary>
        /// 設定を読み込みます。
        /// </summary>
        /// <param name="configurationRoot">読み込みの基準となる <see cref="IConfiguration"/>。</param>
        /// <returns>この <see cref="ISwaggerServerService"/> のインスタンス。</returns>
        public new ISwaggerServerService LoadConfiguration(IConfiguration configurationRoot);
    }
}
