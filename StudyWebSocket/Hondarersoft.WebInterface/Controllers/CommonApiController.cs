using Microsoft.Extensions.Logging;
using System.Linq;
using System.Reflection;

namespace Hondarersoft.WebInterface.Controllers
{
    public abstract class CommonApiController : ICommonApiController
    {
        protected readonly ILogger logger;

        public string ApiPath { get; protected set; }

        public MatchingMethod MatchingMethod { get; protected set; }

        public CommonApiController(ILogger logger)
        {
            this.logger = logger;
            getApiPathAttribute();
        }

        protected void getApiPathAttribute()
        {
            ICustomAttributeProvider provider = GetType();

            ApiPathAttribute apiPathAttribute = provider.GetCustomAttributes(typeof(ApiPathAttribute), true).FirstOrDefault() as ApiPathAttribute;

            ApiPath = apiPathAttribute.ApiPath;
            MatchingMethod = apiPathAttribute.MatchingMethod;
        }

        public virtual void ProcGet(CommonApiArgs apiArgs)
        {
            logger.LogInformation("Get: apiArgs: {0}", apiArgs);
        }

        public virtual void ProcPost(CommonApiArgs apiArgs)
        {
            logger.LogInformation("Post: apiArgs: {0}", apiArgs);
        }

        public virtual void ProcPut(CommonApiArgs apiArgs)
        {
            logger.LogInformation("Put: apiArgs: {0}", apiArgs);
        }

        public virtual void ProcDelete(CommonApiArgs apiArgs)
        {
            logger.LogInformation("Delete: apiArgs: {0}", apiArgs);
        }
    }
}
