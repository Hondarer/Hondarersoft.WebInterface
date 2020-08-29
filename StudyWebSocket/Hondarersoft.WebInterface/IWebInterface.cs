using System;
using System.Collections.Generic;
using System.Text;

namespace Hondarersoft.WebInterface
{
    public interface IWebInterface
    {
        public string Hostname { get; set; }

        public int PortNumber { get; set; }

        public string BasePath { get; set; }

        public bool UseSSL { get; set; }
    }
}
