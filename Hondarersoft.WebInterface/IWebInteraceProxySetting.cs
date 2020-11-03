using System;
using System.Collections.Generic;
using System.Text;

namespace Hondarersoft.WebInterface
{
    public interface IWebInteraceProxySetting
    {

        public bool UseDefaultProxy { get; set; }

        public bool UseCustomProxy { get; set; }

        public string ProxyUrl { get; set; }

        public string ProxyAccount { get; set; }

        public string ProxyPassword { get; set; }
    }
}
