using System;
using System.Collections.Generic;
using System.Text;

namespace Hondarersoft.WebInterface.Controllers
{
    [AttributeUsage(AttributeTargets.Class)]
    public class ApiPathAttribute : Attribute
    {
        public string ApiPath { get; protected set; }

        public ApiPathAttribute(string apiPath)
        {
            this.ApiPath = apiPath;
        }
    }
}
