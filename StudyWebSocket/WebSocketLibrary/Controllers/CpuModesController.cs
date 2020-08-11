using System;
using System.Collections.Generic;
using System.Text;
using WebSocketLibrary.Schemas;

namespace WebSocketLibrary.Controllers
{
    public class CpuModesController : CommonApiController
    {
        public CpuModesController() : base("/cpumodes") // TODO: アトリビュートで指定するほうが良い
        {
        }

        public override void Get(CommonApiArgs apiArgs)
        {
            if (apiArgs.Path.Equals(AcceptPath) == true)
            {
                // 一括取得
                apiArgs.ResponseBody = new CpuModes() { new CpuMode() { Hostname = "localhoost" }, new CpuMode() { Hostname = "hostname2" } };
            }
            else if (apiArgs.Path.Equals(AcceptPath + "/localhost") == true)
            {
                // ID 指定取得
                apiArgs.ResponseBody = new CpuMode() { Hostname = "localhoost" };
            }
        }
    }
}
