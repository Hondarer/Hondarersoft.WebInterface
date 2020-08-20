using Microsoft.Extensions.Logging;
using WebInterfaceLibrary.Schemas;

namespace WebInterfaceLibrary.Controllers
{
    [ApiPath("/cpumodes")]
    public class CpuModesController : CommonApiController
    {
        public CpuModesController(ILogger logger) : base(logger)
        {
        }

        public override void Get(CommonApiArgs apiArgs)
        {
            base.Get(apiArgs);

            if (apiArgs.Path.Equals(ApiPath) == true)
            {
                // 一括取得
                apiArgs.ResponseBody = new CpuModes() { new CpuMode() { Hostname = "localhost" }, new CpuMode() { Hostname = "hostname2" } };
            }
            else if (apiArgs.Path.Equals(ApiPath + "/localhost") == true)
            {
                // ID 指定取得
                apiArgs.ResponseBody = new CpuMode() { Hostname = "localhost" };
            }
        }
    }
}
