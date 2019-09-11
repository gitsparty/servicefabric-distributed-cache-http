namespace Cache.Client
{
    using System;
    using System.Fabric;
    using System.Threading;
    using System.Threading.Tasks;
    using System.Net.Http;
    using Cache.Abstractions;
    using Microsoft.ServiceFabric.Services.Communication.Client;

    public class CacheCommunicationClient : ICommunicationClient, ICache
    {
        public CacheCommunicationClient(
            CancellationToken token,
            HttpClient httpClient,
            string address,
            ILocalCache localCache)
        {
            this.HttpClient = httpClient;
            this.Url = new Uri(address);
            this.LocalCache = localCache;
            this.CancellationToken = token;
        }

        public CancellationToken CancellationToken { get; private set; }

        public HttpClient HttpClient { get; private set; }

        public Uri Url { get; private set; }

        public ILocalCache LocalCache { get; private set; }

        ResolvedServiceEndpoint ICommunicationClient.Endpoint { get; set; }

        string ICommunicationClient.ListenerName { get; set; }

        ResolvedServicePartition ICommunicationClient.ResolvedServicePartition { get; set; }

        public async Task<byte[]> GetAsync(
            string key,
            CancellationToken token = default(CancellationToken))
        {
            if (LocalCache != null)
            {
                return await LocalCache.GetAsync(key, token);
            }
            else
            {
                var builder = new UriBuilder(Url);
                builder.Path = $"/api/cache/{key}";
                return await HttpClient.GetByteArrayAsync(builder.Uri);
            }
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
            if (LocalCache != null)
            {
                return LocalCache.RefreshAsync(key, token);
            }
            else
            {
                var builder = new UriBuilder(Url);
                builder.Path = $"/api/cache/{key}";
                return HttpClient.DeleteAsync(builder.Uri);
            }
        }

        public async Task SetAsync(
            string key, byte[] value,
            CancellationToken token = default(CancellationToken))
        {
            if (LocalCache != null)
            {
                await LocalCache.SetAsync(key, value, token);
            }
            else
            {
                var builder = new UriBuilder(Url);
                builder.Path = $"/api/cache/{key}";
                var byteContent = new ByteArrayContent(value);
                await HttpClient.PutAsync(builder.Uri, byteContent);
            }
        }

        public async Task<byte[]> CreateCachedItemAsync(
            string key,
            byte[] value,
            CancellationToken token)
        {
            if (LocalCache != null)
            {
                return await LocalCache.CreateCachedItemAsync(key, value, token);
            }
            else
            {
                var builder = new UriBuilder(Url);
                builder.Path = $"/api/cache/{key}";
                var byteContent = new ByteArrayContent(value);
                var ret = await HttpClient.PostAsync(builder.Uri, byteContent);
                return await ret.Content.ReadAsByteArrayAsync();
            }
        }
    }
}