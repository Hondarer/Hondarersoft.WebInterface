using Hondarersoft.WebInterface.Sample.Schemas;
using Hondarersoft.WebInterface.Test;
using System;
using System.Collections.Generic;
using System.Text;
using Xunit;

namespace Hondarersoft.WebInterface.Sample.Test.Controllers
{
    public class CpuModesControllerTest
    {
        [Fact]
        public void Test()
        {
            Sample.Controllers.CpuModesController CpuModesController = new Sample.Controllers.CpuModesController(new TestLogger());

            CommonApiArgs apiArgs = new CommonApiArgs("TestID", CommonApiArgs.Methods.GET, "/cpumodes/localhost");

            CpuModesController.Get(apiArgs);

            Assert.True(apiArgs.Handled);
            Assert.True(apiArgs.ResponseBody is CpuMode);

            CpuMode response = apiArgs.ResponseBody as CpuMode;

            Assert.Equal("localhost", response.Hostname);
            Assert.Equal(0, response.Modecode);
        }
    }
}
