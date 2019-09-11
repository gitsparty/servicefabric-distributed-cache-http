using System.Threading;
using System.Threading.Tasks;
using System.Fabric;
using Cache.Abstractions;
using Microsoft.ServiceFabric.Services.Communication.Client;

namespace Cache.Client
{
    public class CacheServiceClient : ICacheServiceClient
    {
        ICachePartitionClient _partitionClient;

        public CacheServiceClient(ICachePartitionClient client)
        {
            _partitionClient = client;
        }

        public Task<byte[]> GetAsync(
            string key,
            CancellationToken token = default(CancellationToken))
        {
            return _partitionClient.InvokeWithRetryAsync(
                    (client) =>
                    {
                        return client.GetAsync(key, client.CancellationToken);
                    },
                    token);
        }

        public Task RefreshAsync(
            string key,
            CancellationToken token = default(CancellationToken))
        {
            return GetAsync(key, token);
        }

        public Task RemoveAsync(
            string key,
            CancellationToken token = default(CancellationToken))
        {
            return _partitionClient.InvokeWithRetryAsync(
                (client) =>
                {
                    return client.RemoveAsync(key, client.CancellationToken);
                },
                token);
        }

        public Task SetAsync(
            string key, byte[] value,
            CancellationToken token = default(CancellationToken))
        {
            return _partitionClient.InvokeWithRetryAsync(
                (client) =>
                {
                    return client.SetAsync(key, value, client.CancellationToken);
                },
                token);
        }

        public Task<byte[]> CreateCachedItemAsync(
            string key,
            byte[] value,
            CancellationToken token)
        {
            return _partitionClient.InvokeWithRetryAsync(
                (client) =>
                {
                    return client.CreateCachedItemAsync(key, value, client.CancellationToken);
                },
                token);
        }
    }
}
