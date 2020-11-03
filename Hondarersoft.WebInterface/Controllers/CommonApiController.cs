using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;

namespace Hondarersoft.WebInterface.Controllers
{
    public abstract class CommonApiController : ICommonApiController
    {
        protected readonly ILogger _logger;

        public string ApiPath { get; protected set; }

        public MatchingMethod MatchingMethod { get; protected set; }

        public CommonApiController(ILogger logger)
        {
            _logger = logger;
            GetApiPathAttribute();
        }

        protected void GetApiPathAttribute()
        {
            ICustomAttributeProvider provider = GetType();

            ApiPathAttribute apiPathAttribute = provider.GetCustomAttributes(typeof(ApiPathAttribute), true).FirstOrDefault() as ApiPathAttribute;

            ApiPath = apiPathAttribute.ApiPath;
            MatchingMethod = apiPathAttribute.MatchingMethod;
        }

        public void Proc(CommonApiArgs apiArgs)
        {
            bool isMatch = false;
            switch (MatchingMethod)
            {
                case MatchingMethod.StartsWith:
                    if (apiArgs.Path.StartsWith(ApiPath) == true)
                    {
                        isMatch = true;
                    }
                    break;
                case MatchingMethod.Equals:
                    if (apiArgs.Path == ApiPath)
                    {
                        isMatch = true;
                    }
                    break;
                case MatchingMethod.RegEx:
                    Regex regex = new Regex(ApiPath, RegexOptions.Singleline);
                    Match match = regex.Match(apiArgs.Path);
                    if (match.Success == true)
                    {
                        // 正規表現でグループ指定がされていた場合は、グループの値を apiArgs に設定する。
                        apiArgs.RegExMatchGroups = new Dictionary<string, string>();
                        foreach (Group group in match.Groups.Skip(1))
                        {
                            apiArgs.RegExMatchGroups.Add(group.Name, group.Value);
                        }
                        isMatch = true;
                    }
                    break;
                default:
                    break;
            }

            if (isMatch == true)
            {
                switch (apiArgs.Method)
                {
                    case CommonApiMethods.GET:
                        ProcGet(apiArgs);
                        break;
                    case CommonApiMethods.POST:
                        ProcPost(apiArgs);
                        break;
                    case CommonApiMethods.PUT:
                        ProcPut(apiArgs);
                        break;
                    case CommonApiMethods.DELETE:
                        ProcDelete(apiArgs);
                        break;
                    default:
                        break;
                }
            }
        }

        protected virtual void ProcGet(CommonApiArgs apiArgs)
        {
        }

        protected virtual void ProcPost(CommonApiArgs apiArgs)
        {
        }

        protected virtual void ProcPut(CommonApiArgs apiArgs)
        {
        }

        protected virtual void ProcDelete(CommonApiArgs apiArgs)
        {
        }
    }
}
