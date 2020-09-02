using System;
using System.Collections.Generic;
using System.Text;

namespace Hondarersoft.WebInterface.Controllers
{
    [AttributeUsage(AttributeTargets.Class)]
    public class ApiPathAttribute : Attribute
    {
        public string ApiPath { get; protected set; }

        public MatchingMethod MatchingMethod { get; protected set; }

        public ApiPathAttribute(string apiPath, MatchingMethod matchingMethod = MatchingMethod.StartsWith)
        {
            ApiPath = apiPath;
            MatchingMethod = matchingMethod;
        }
    }
}
