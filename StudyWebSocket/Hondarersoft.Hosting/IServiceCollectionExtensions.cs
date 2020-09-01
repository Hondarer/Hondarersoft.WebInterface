using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using System.Text.Json;

namespace Hondarersoft.Hosting
{
    public static class IServiceCollectionExtensions
    {
        public static IServiceCollection AddServiceFromConfigration(this IServiceCollection serviceCollection, IConfiguration configurationRoot)
        {

            ServiceConfigEntry[] serviceConfig = configurationRoot.GetSection("AddServices").Get<ServiceConfigEntry[]>();

            if (serviceConfig == null)
            {
                return serviceCollection;
            }

            foreach (ServiceConfigEntry entry in serviceConfig)
            {
                // TODO: いいかげん(ちゃんと例外処理をすること)
                Assembly asm = Assembly.Load(entry.AssemblyName);
                Type interfaceType = asm.GetType(entry.InterfaceFullName);
                Type classType = asm.GetType(entry.ClassFullName);

                if (entry.IsSingleton == true)
                {
                    serviceCollection.AddSingleton(interfaceType, classType);
                }
                else
                {
                    serviceCollection.AddTransient(interfaceType, classType);
                }
            }

            return serviceCollection;
        }
    }
}
