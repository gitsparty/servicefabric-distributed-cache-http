﻿using System;
using System.Fabric;
using Cache.Abstractions;
using Cache.Client;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Internal;
using Microsoft.ServiceFabric.Services.Communication.Client;

namespace Cache.StatefulCache
{
    public static class Extensions
    {
        public static IServiceCollection AddDistributedServiceFabricCache(
            this IServiceCollection services)
        {
            if (services == null) throw new ArgumentNullException(nameof(services));

            services.AddScoped<IRequestContext, Request>();
            return services;
        }
    }
}
