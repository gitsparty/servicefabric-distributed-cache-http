using System;
using System.Fabric;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Internal;

namespace Cache.StatefulCache
{
    public static class Extensions
    {
        public static IServiceCollection AddDistributedServiceFabricCache(
            this IServiceCollection services)
        {
            if (services == null) throw new ArgumentNullException(nameof(services));

            return services;
        }
    }
}
