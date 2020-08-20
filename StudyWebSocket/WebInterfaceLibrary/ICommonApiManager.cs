using System;
using System.Collections.Generic;
using System.Text;

namespace WebInterfaceLibrary
{
    public interface ICommonApiManager
    {
        public ICommonApiManager Start();

        public ICommonApiManager Regist(WebInterfaceBase webInterfaceBase);
    }
}
