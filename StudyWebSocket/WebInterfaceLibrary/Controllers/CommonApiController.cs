using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Text;

namespace WebInterfaceLibrary.Controllers
{
    public abstract class CommonApiController
    {
        //protected readonly ILogger logger;

        public string AcceptPath { get; private set; }

        //public CommonApiController(ILogger logger, string acceptPath)
        //{
        //    this.logger = logger;
        //    AcceptPath = acceptPath;
        //}

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
