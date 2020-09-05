using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Hondarersoft.WebInterface.Swagger
{
    public interface ISwaggerServerService : IWebApiService
    {
        public Func<Stream> SwaggerYamlResolver { get; set; }

        public ISwaggerServerService SetSwaggerYamlResolver(Func<Stream> swaggerYamlResolver);

        public ISwaggerServerService LoadConfiguration(IConfiguration configurationRoot);
    }
}
