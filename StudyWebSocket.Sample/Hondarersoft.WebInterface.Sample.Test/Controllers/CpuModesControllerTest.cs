using System;
using System.Collections.Generic;
using System.Text;
using Xunit;
using WebInterfaceLibrary.Controllers;
using WebInterfaceLibrary;
using WebInterfaceLibrary.Schemas;

namespace Hondarersoft.WebInterface.Sample.Test.Controllers
{
    public class CpuModesControllerTest
    {
        [Fact]
        public void Test()
        {
            CpuModesController CpuModesController = new CpuModesController(new TestLogger());

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
