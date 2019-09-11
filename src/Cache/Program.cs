using System;
using System.Diagnostics;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.ServiceFabric.Services.Runtime;

namespace Cache
{
    internal static class Program
    {
        /// <summary>
        /// This is the entry point of the service host process.
        /// </summary>
        private static void Main()
        {
            try
            {
                ConfigureServicePoints();

                // The ServiceManifest.XML file defines one or more service type names.
                // Registering a service maps a service type name to a .NET type.
                // When Service Fabric creates an instance of this service type,
                // an instance of the class is created in this host process.

                ServiceRuntime.RegisterServiceAsync("CacheType",
                    context => new StatefulCache.CacheStatefulService(context)).GetAwaiter().GetResult();

                ServiceEventSource.Current.ServiceTypeRegistered(Process.GetCurrentProcess().Id, typeof(StatefulCache.CacheStatefulService).Name);

                // Prevents this host process from terminating so services keep running.
                Thread.Sleep(Timeout.Infinite);
            }
            catch (Exception e)
            {
                ServiceEventSource.Current.ServiceHostInitializationFailed(e.ToString());
                throw;
            }
        }

        private static void ConfigureServicePoints()
        {
            // Tean down the service point afer an hour if there are no connections
            ServicePointManager.MaxServicePointIdleTime = 60 * 60 * 1000;
            ServicePointManager.UseNagleAlgorithm = false;
            ServicePointManager.Expect100Continue = false;
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
            ServicePointManager.DefaultConnectionLimit = 200;

            // Azure LB times out a TCP connection in 4 minutes. Keep the connection alive.
            // https://docs.microsoft.com/en-us/azure/load-balancer/load-balancer-tcp-idle-timeout
            ServicePointManager.SetTcpKeepAlive(true, 15000, 15000);
        }
    }
}
