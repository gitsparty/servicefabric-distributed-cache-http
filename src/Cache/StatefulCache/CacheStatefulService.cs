using System;
using System.Collections.Generic;
using System.Fabric;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Cache.Abstractions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.ServiceFabric.Data.Collections;
using Microsoft.ServiceFabric.Services.Communication.AspNetCore;
using Microsoft.ServiceFabric.Services.Communication.Runtime;
using Microsoft.ServiceFabric.Services.Runtime;

namespace Cache.StatefulCache
{
    /// <summary>
    /// An instance of this class is created for each service replica by the Service Fabric runtime.
    /// </summary>
    internal sealed class CacheStatefulService : StatefulService, ILocalCache
    {
        private const string cacheName = "Distributed.Cache";

        public CacheStatefulService(StatefulServiceContext context)
            : base(context)
        { }

        /// <summary>
        /// Optional override to create listeners (e.g., HTTP, Service Remoting, WCF, etc.) for this service replica to handle client or user requests.
        /// </summary>
        /// <remarks>
        /// For more information on service communication, see https://aka.ms/servicefabricservicecommunication
        /// </remarks>
        /// <returns>A collection of listeners.</returns>
        protected override IEnumerable<ServiceReplicaListener> CreateServiceReplicaListeners()
        {
            return new ServiceReplicaListener[]
            {
                new ServiceReplicaListener(
                    name: "Primary",
                    listenOnSecondary: false,
                    createCommunicationListener:
                        serviceContext =>
                            new KestrelCommunicationListener(
                                serviceContext: serviceContext,
                                endpointName: "ServiceEndpointPrimary",
                                build: (url, listener) =>
                                {
                                    ServiceEventSource.Current.ServiceMessage(serviceContext, $"Starting primary kestrel on {url}");

                                    return new WebHostBuilder()
                                                .UseKestrel()
                                                .ConfigureServices(
                                                    services => services
                                                        .AddSingleton<IStatefulContext>(new StatefulContextWrapper(this.Context))
                                                        .AddSingleton<ILocalCache>(this))
                                                .UseContentRoot(Directory.GetCurrentDirectory())
                                                .UseStartup<Cache.StatefulCache.Startup>()
                                                .UseServiceFabricIntegration(listener, ServiceFabricIntegrationOptions.None)
                                                .UseUrls(url)
                                                .Build();
                                }))
            };
        }

        public async Task<byte[]> GetAsync(string key, CancellationToken token = default(CancellationToken))
        {
            var cache = await this.StateManager.GetOrAddAsync<IReliableDictionary<string, byte[]>>(cacheName);

            using (var tx = this.StateManager.CreateTransaction())
            {
                var val = await cache.TryGetValueAsync(tx, key, timeout: TimeSpan.FromSeconds(4), cancellationToken: token);
                return val.Value;
            }
        }

        public Task RefreshAsync(string key, CancellationToken token = default(CancellationToken))
        {
            return this.GetAsync(key, token);
        }

        public async Task RemoveAsync(string key, CancellationToken token = default(CancellationToken))
        {
            var cache = await this.StateManager.GetOrAddAsync<IReliableDictionary<string, byte[]>>(cacheName);

            using (var tx = this.StateManager.CreateTransaction())
            {
                await cache.TryRemoveAsync(tx, key, timeout: TimeSpan.FromSeconds(4), cancellationToken: token);
            }
        }

        public async Task SetAsync(
            string key,
            byte[] value,
            CancellationToken token = default(CancellationToken))
        {
            var cache = await this.StateManager.GetOrAddAsync<IReliableDictionary<string, byte[]>>(cacheName);

            using (var tx = this.StateManager.CreateTransaction())
            {
                await cache.SetAsync(tx, key, value, timeout: TimeSpan.FromSeconds(4), cancellationToken: token);
            }
        }

        public async Task<byte[]> CreateCachedItemAsync(
            string key,
            byte[] value,
            CancellationToken token)
        {
            var cache = await this.StateManager.GetOrAddAsync<IReliableDictionary<string, byte[]>>(cacheName);

            using (var tx = this.StateManager.CreateTransaction())
            {
                try
                {
                    var added = await cache.TryAddAsync(
                        tx,
                        key,
                        value,
                        timeout: TimeSpan.FromSeconds(4),
                        cancellationToken: token);

                    if (added)
                    {
                        await tx.CommitAsync();
                        return value;
                    }
                    else
                    {
                        return null;
                    }
                }
                catch (Exception ex)
                {
                    return null;
                }
            }
        }
    }
}
