using System;
using System.Collections.Generic;
using System.Text;

namespace Hondarersoft.WebInterface.Controllers
{
    public interface ICommonApiController
    {
        public string ApiPath { get; }

        public MatchingMethod MatchingMethod { get; }

        public void ProcGet(CommonApiArgs apiArgs);

        public void ProcPost(CommonApiArgs apiArgs);

        public void ProcPut(CommonApiArgs apiArgs);

        public void ProcDelete(CommonApiArgs apiArgs);
    }
}
