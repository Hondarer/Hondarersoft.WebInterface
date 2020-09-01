using System;
using System.Collections.Generic;
using System.Text;

namespace Hondarersoft.Hosting
{
    public class ServiceConfigEntry
    {
        public string AssemblyName { get; set; }

        public string InterfaceFullName { get; set; }

        public string ClassFullName { get; set; }

        public bool IsSingleton { get; set; }
    }
}
