using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

namespace WebInterfaceLibrary.Controllers
{
    public abstract class CommonApiController : ICommonApiController
    {
        protected readonly ILogger logger;

        public string ApiPath { get; protected set; }

        public CommonApiController(ILogger logger)
        {
            this.logger = logger;
            ApiPath = getApiPathAttribute();
        }

        protected string getApiPathAttribute()
        {
            ICustomAttributeProvider provider = GetType();

            ApiPathAttribute apiPathAttribute = provider.GetCustomAttributes(typeof(ApiPathAttribute), true).FirstOrDefault() as ApiPathAttribute;

            return apiPathAttribute.ApiPath;
        }

        public virtual void Get(CommonApiArgs apiArgs)
        {
            logger.LogInformation("Get: apiArgs: {0}", apiArgs);
        }

        public virtual void Post(CommonApiArgs apiArgs)
        {
            logger.LogInformation("Post: apiArgs: {0}", apiArgs);
        }

        public virtual void Put(CommonApiArgs apiArgs)
        {
            logger.LogInformation("Put: apiArgs: {0}", apiArgs);
        }

        public virtual void Delete(CommonApiArgs apiArgs)
        {
            logger.LogInformation("Delete: apiArgs: {0}", apiArgs);
        }
    }
}
