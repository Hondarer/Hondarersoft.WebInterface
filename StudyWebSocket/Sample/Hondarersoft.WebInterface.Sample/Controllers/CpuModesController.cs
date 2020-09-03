using Hondarersoft.WebInterface.Controllers;
using Hondarersoft.WebInterface.Sample.Schemas;
using Microsoft.Extensions.Logging;

namespace Hondarersoft.WebInterface.Sample.Controllers
{
    [ApiPath("/api/v1/cpumodes")]
    public class CpuModesController : CommonApiController
    {
        public CpuModesController(ILogger<CpuModesController> logger) : base(logger)
        {
        }

        protected override void ProcGet(CommonApiArgs apiArgs)
        {
            base.ProcGet(apiArgs);

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
