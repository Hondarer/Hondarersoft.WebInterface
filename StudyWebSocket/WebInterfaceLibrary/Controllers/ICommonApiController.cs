using System;
using System.Collections.Generic;
using System.Text;

namespace WebInterfaceLibrary.Controllers
{
    public interface ICommonApiController
    {
        public string ApiPath { get; }

        public void Get(CommonApiArgs apiArgs);

        public void Post(CommonApiArgs apiArgs);

        public void Put(CommonApiArgs apiArgs);

        public void Delete(CommonApiArgs apiArgs);
    }
}
