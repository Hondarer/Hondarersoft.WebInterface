using System;
using System.Collections.Generic;
using System.Text;

namespace Hondarersoft.WebInterface
{
    public class CommonApiRequest
    {
        public string InterfaceIdentify { get; set; }

        public string SessionIdentify { get; set; }

        public CommonApiMethods Method { get; set; }

        public string Path { get; set; }

        public string Params { get; set; }

        public TimeSpan Timeout { get; set; } = TimeSpan.FromSeconds(3);

        public bool NotifyOnly { get; set; } = false;
    }
}
