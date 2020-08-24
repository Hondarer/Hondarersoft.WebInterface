using System;
using System.Collections.Generic;
using System.Text;

namespace Hondarersoft.WebInterface
{
    public interface ICommonApiManager
    {
        public ICommonApiManager Start();

        public ICommonApiManager Regist(WebInterfaceBase webInterfaceBase);
    }
}
