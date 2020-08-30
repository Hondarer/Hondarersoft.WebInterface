using Microsoft.CSharp.RuntimeBinder;
using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Text;

namespace Hondarersoft.Utility
{
    public static class DynamicHelper
    {
        public static bool IsPropertyExist(dynamic target, string name)
        {
            if (target is ExpandoObject)
            {
                return ((IDictionary<string, object>)target).ContainsKey(name);
            }

            return target.GetType().GetProperty(name) != null;
        }

        public static object GetProperty(dynamic target, string name)
        {
            var site = System.Runtime.CompilerServices.CallSite<Func<System.Runtime.CompilerServices.CallSite, object, object>>.Create(Binder.GetMember(0, name, target.GetType(), new[] { CSharpArgumentInfo.Create(0, null) }));
            return site.Target(site, target);
        }

        public static void AddProperty(dynamic target,string name, object value)
        {
            if (target is ExpandoObject)
            {
                ((IDictionary<string, object>)target).Add(name, value);
            }
            else
            {
                throw new InvalidOperationException("target is not ExpandoObject.");
            }
        }
    }
}
