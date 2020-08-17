using System;
using System.Collections.Generic;
using System.Text;

namespace WebInterfaceLibrary.Controllers
{
    public abstract class CommonApiController
    {
        public string AcceptPath { get; private set; }

        public CommonApiController(string acceptPath)
        {
            AcceptPath = acceptPath;
        }

        public virtual void Get(CommonApiArgs apiArgs)
        {
        }

        public virtual void Post(CommonApiArgs apiArgs)
        {
        }

        public virtual void Put(CommonApiArgs apiArgs)
        {
        }

        public virtual void Delete(CommonApiArgs apiArgs)
        {
        }
    }
}
