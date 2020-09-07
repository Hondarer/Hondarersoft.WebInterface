﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Hondarersoft.Hosting
{
    public static class IServiceProviderExtensions
    {
        private static readonly Dictionary<Tuple<string, string>, object> singletonCache = new Dictionary<Tuple<string, string>, object>();

        public static object GetService(this IServiceProvider serviceProvider, string assemblyName, string classFullName, bool isSingleton = false)
        {
            if (isSingleton == true)
            {
                Tuple<string, string> key = new Tuple<string, string>(assemblyName, classFullName);

                lock (singletonCache)
                {
                    if (singletonCache.ContainsKey(key) == true)
                    {
                        return singletonCache[key];
                    }
                    else
                    {
                        object service = GetServiceCore(serviceProvider, assemblyName, classFullName);
                        singletonCache.Add(key, service);

                        return service;
                    }
                }
            }

            return GetServiceCore(serviceProvider, assemblyName, classFullName);
        }

        private static object GetServiceCore(IServiceProvider serviceProvider, string assemblyName, string classFullName)
        {
            // 各インターフェースは DI コンテナから払い出したいので、ここで払い出し処理を行う。
            // TODO: 各種例外への対応ができていない。

            Assembly asm = Assembly.Load(assemblyName);
            Type commonApiControllerType = asm.GetType(classFullName);

            List<Type> types = new List<Type>();
            List<object> objects = new List<object>();

            // TODO: 厳密な DI ルールは、InjectionConstructor のあるものを優先、引数の多いもの優先、引数の多いものが複数あったら例外

            foreach (ParameterInfo parameter in commonApiControllerType.GetConstructors().First().GetParameters())
            {
                types.Add(parameter.ParameterType);
                objects.Add(serviceProvider.GetService(parameter.ParameterType));
            }
            ConstructorInfo constructor = commonApiControllerType.GetConstructor(types.ToArray());
            return constructor.Invoke(objects.ToArray());
        }
    }
}
