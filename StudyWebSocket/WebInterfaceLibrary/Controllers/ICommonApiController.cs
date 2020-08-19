using System;
using System.Collections.Generic;
using System.Text;

namespace WebInterfaceLibrary.Controllers
{
    public interface ICommonApiController
    {
        public void Get(CommonApiArgs apiArgs);
    }
}
