using System.Threading;
using System.Threading.Tasks;
using System.Fabric;
using Cache.Abstractions;
using Microsoft.ServiceFabric.Services.Communication.Client;

namespace Cache.Client
{
    public class CacheServiceClient : ICacheServiceClient
    {
        IRequestContext _context;
        ICachePartitionClient _partitionClient;

        public CacheServiceClient(
            IRequestContext context,
            ICachePartitionClient client)
        {
            _context = context;
            _partitionClient = client;
        }

        public Task<byte[]> GetAsync(
            string key,
            CancellationToken token = default(CancellationToken))
        {
            return _partitionClient.InvokeWithRetryAsync(
                    (client) =>
                    {
                        ServiceEventSource.Current.ServiceMessage(_context, $"CacheServiceClient::GetAsync: GetAsync {key}");
                        return client.GetAsync(key, token);
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
                    ServiceEventSource.Current.ServiceMessage(_context, $"RemoveAsync::GetAsync: GetAsync {key}");
                    return client.RemoveAsync(key, token);
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
                    ServiceEventSource.Current.ServiceMessage(_context, $"RemoveAsync::SetAsync: GetAsync {key}");
                    return client.SetAsync(key, value, token);
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
                    ServiceEventSource.Current.ServiceMessage(_context, $"CreateCachedItemAsync::SetAsync: GetAsync {key}");
                    return client.CreateCachedItemAsync(key, value, token);
                },
                token);
        }
    }
}
