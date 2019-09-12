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
        private ICacheSvcStateContext _context;

        public CacheCommunicationClient(
            CancellationToken token,
            IRequestContext context,
            HttpClient httpClient,
            string address,
            ILocalCache localCache)
        {
            this.HttpClient = httpClient;
            this.Url = new Uri(address);
            this.LocalCache = localCache;
            this.CancellationToken = token;
            _context = context;
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
                ServiceEventSource.Current.ServiceMessage(_context, $"CacheCommunicationClient::GetAsync: Local: {key}");
                return await LocalCache.GetAsync(key, token);
            }
            else
            {
                var builder = new UriBuilder(Url);
                builder.Path = $"/api/cache/{key}";

                ServiceEventSource.Current.ServiceMessage(_context, $"CacheCommunicationClient::GetAsync: Remote: {builder.Uri}");
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
                ServiceEventSource.Current.ServiceMessage(_context, $"CacheCommunicationClient::RemoveAsync: Local: {key}");
                return LocalCache.RemoveAsync(key, token);
            }
            else
            {
                var builder = new UriBuilder(Url);
                builder.Path = $"/api/cache/{key}";
                ServiceEventSource.Current.ServiceMessage(_context, $"CacheCommunicationClient::RemoveAsync: Remote: {builder.Uri}");
                return HttpClient.DeleteAsync(builder.Uri);
            }
        }

        public async Task SetAsync(
            string key,
            byte[] value,
            CancellationToken token = default(CancellationToken))
        {
            if (LocalCache != null)
            {
                ServiceEventSource.Current.ServiceMessage(_context, $"CacheCommunicationClient::SetAsync: Local: {key}");
                await LocalCache.SetAsync(key, value, token);
            }
            else
            {
                var builder = new UriBuilder(Url);
                builder.Path = $"/api/cache/{key}";
                ServiceEventSource.Current.ServiceMessage(_context, $"CacheCommunicationClient::SetAsync: Remote: {builder.Uri}");
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
                ServiceEventSource.Current.ServiceMessage(_context, $"CacheCommunicationClient::CreateCachedItemAsync: Local: {key}");
                return await LocalCache.CreateCachedItemAsync(key, value, token);
            }
            else
            {
                var builder = new UriBuilder(Url);
                builder.Path = $"/api/cache/{key}";
                ServiceEventSource.Current.ServiceMessage(_context, $"CacheCommunicationClient::CreateCachedItemAsync: Remote: {builder.Uri}");

                var byteContent = new ByteArrayContent(value);
                var ret = await HttpClient.PostAsync(builder.Uri, byteContent);
                return await ret.Content.ReadAsByteArrayAsync();
            }
        }

        public override string ToString()
        {
            if (LocalCache != null)
            {
                return $"Local Cache communication client. Url =  {this.Url}";
            }
            else
            {
                if (HttpClient != null)
                {
                    return $"Remote Cache communication client. Url = {this.Url}";
                }
                else
                {
                    return $"Uninitialized Client. Url = {this.Url}";
                }
            }
        }
    }
}