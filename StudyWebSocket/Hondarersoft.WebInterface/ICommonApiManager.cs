using System;
using System.Collections.Generic;
using System.Text;

namespace Hondarersoft.WebInterface
{
    public interface ICommonApiManager
    {
        public ICommonApiManager Start();

        public ICommonApiManager RegistController(string assemblyName, string classFullName);


        public ICommonApiManager RegistInterface(IWebInterface webInterfaceBase, string identify = null);
    }
}
