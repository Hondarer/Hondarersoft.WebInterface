using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Text;
using WebInterfaceLibrary.Schemas;

namespace WebInterfaceLibrary.Controllers
{
    public class CpuModesController : CommonApiController
    {
        public CpuModesController(ILogger logger) : base(logger, "/cpumodes") // TODO: アトリビュートで指定するほうが良い
        {
        }

        public override void Get(CommonApiArgs apiArgs)
        {
            base.Get(apiArgs);

            if (apiArgs.Path.Equals(AcceptPath) == true)
            {
                // 一括取得
                apiArgs.ResponseBody = new CpuModes() { new CpuMode() { Hostname = "localhost" }, new CpuMode() { Hostname = "hostname2" } };
            }
            else if (apiArgs.Path.Equals(AcceptPath + "/localhost") == true)
            {
                // ID 指定取得
                apiArgs.ResponseBody = new CpuMode() { Hostname = "localhost" };
            }
        }
    }
}
