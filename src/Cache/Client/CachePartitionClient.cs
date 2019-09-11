using System;
using System.Threading;
using System.Threading.Tasks;
using System.Fabric;
using Cache.Abstractions;
using Microsoft.ServiceFabric.Services.Communication.Client;

namespace Cache.Client
{
    public class CachePartitionClient : ICachePartitionClient
    {
        ServicePartitionClient<CacheCommunicationClient> _partitionClient;

        public CachePartitionClient(ServicePartitionClient<CacheCommunicationClient> client)
        {
            _partitionClient = client;
        }

        public Task<TResult> InvokeWithRetryAsync<TResult>(
            Func<CacheCommunicationClient, Task<TResult>> func,
            CancellationToken cancellationToken, 
            params Type[] doNotRetryExceptionTypes)
        {
            return _partitionClient.InvokeWithRetryAsync(func, cancellationToken, doNotRetryExceptionTypes);
        }

        public Task InvokeWithRetryAsync(
            Func<CacheCommunicationClient, Task> func,
            CancellationToken cancellationToken,
            params Type[] doNotRetryExceptionTypes)
        {
            return _partitionClient.InvokeWithRetryAsync(func, cancellationToken, doNotRetryExceptionTypes);
        }

    }
}
