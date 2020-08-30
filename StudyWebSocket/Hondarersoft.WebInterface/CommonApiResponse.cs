using Hondarersoft.WebInterface.Schemas;
using System;
using System.Collections.Generic;
using System.Text;

namespace Hondarersoft.WebInterface
{
    public class CommonApiResponse
    {
        public bool IsSuccess { get; set; }

        public object ResponseBody { get; set; }

        public Error Error { get; set; }
    }
}
