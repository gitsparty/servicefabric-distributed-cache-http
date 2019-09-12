using System;
using System.IO;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Cache.Abstractions;
using Cache.Client;
using Microsoft.AspNetCore.Mvc;
using System.Fabric;
using System.Fabric.Query;
using System.Linq;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.ServiceFabric.Services.Communication.Client;
using Microsoft.ServiceFabric.Services.Client;

namespace Cache.StatefulCache
{
    [Route("api/[controller]")]
    [ApiController]
    public class CacheController : ControllerBase
    {
        private readonly CacheCommunicationClientFactory _clientFactory;
        private readonly IRequestContext _context;
        private readonly ILocalCache _localCache;
        private static FabricClient _fabricClient;
        private static ServicePartitionList _servicePartitionList;
        private static DateTime _lastInitializeTime = DateTime.MinValue;
        private object _lock = new object();

        public CacheController(
            IRequestContext context,
            ILocalCache localCache)
        {
            _clientFactory = new CacheCommunicationClientFactory(context, localCache);
            _context = context;
            _localCache = localCache;
        }

        [HttpGet("BaselinePerf")]
        public Task<ActionResult<string>> BaselinePerf(CancellationToken cancellationToken)
        {
            ActionResult<string> result = Content("nas");
            return Task.FromResult(result);
        }

        [HttpGet("{key}")]
        public async Task<ActionResult<string>> Get(string key, CancellationToken cancellationToken)
        {
            ServiceEventSource.Current.ServiceMessage(_context, $"CacheController::Get {this.HttpContext.Request.GetEncodedUrl()}");

            try
            {
                var client = await GetCacheServiceClient(key);
                var bytes = await client.GetAsync(key, cancellationToken);

                if (bytes != null)
                {
                    return Content(Encoding.UTF8.GetString(bytes));
                }

                return new NotFoundResult();
            }
            catch (Exception ex)
            {
                ServiceEventSource.Current.ServiceMessage(_context, $"CacheController::Get Exception {ex}");
                var res = new ObjectResult(ex);
                res.StatusCode = (int)HttpStatusCode.InternalServerError;
                return res;
            }
        }

        [HttpPut("{key}")]
        public async Task Put(string key, CancellationToken cancellationToken)
        {
            var request = HttpContext.Request;
            ServiceEventSource.Current.ServiceMessage(_context, $"CacheController::PUT {request.GetEncodedUrl()}");

            using (var reader = new StreamReader(request.Body))
            {
                var content = await reader.ReadToEndAsync();

                var client = await GetCacheServiceClient(key);
                await client.SetAsync(key, Encoding.UTF8.GetBytes(content), cancellationToken);
            }
        }

        [HttpPost("{key}")]
        public async Task<ActionResult<string>> Post(string key, CancellationToken cancellationToken)
        {
            var request = HttpContext.Request;

            ServiceEventSource.Current.ServiceMessage(_context, $"CacheController::POST {request.GetEncodedUrl()}");

            using (var reader = new StreamReader(request.Body))
            {
                var content = await reader.ReadToEndAsync();

                try
                {
                    var client = await GetCacheServiceClient(key);
                    var result = await client.CreateCachedItemAsync(key, Encoding.UTF8.GetBytes(content), cancellationToken);

                    if (result == null)
                    {
                        return new ConflictResult();
                    }

                    return Created("Created", Encoding.UTF8.GetString(result));
                }
                catch (Exception ex)
                {
                    ServiceEventSource.Current.ServiceMessage(_context, $"CacheController::Post Exception {ex}");
                    var res = new ObjectResult(ex);
                    res.StatusCode = (int)HttpStatusCode.InternalServerError;
                    return res;
                }
            }
        }

        [HttpDelete("{key}")]
        public async Task<ActionResult<string>> Delete(string key, CancellationToken cancellationToken)
        {
            var request = HttpContext.Request;
            ServiceEventSource.Current.ServiceMessage(_context, $"CacheController::PUT {request.GetEncodedUrl()}");

            try
            {
                var client = await GetCacheServiceClient(key);
                await client.RemoveAsync(key, cancellationToken);

                var res = new ObjectResult("Deleted");
                res.StatusCode = (int)HttpStatusCode.OK;
                return res;
            }
            catch (Exception ex)
            {
                ServiceEventSource.Current.ServiceMessage(_context, $"CacheController::Put Exception {ex}");
                var res = new ObjectResult(ex);
                res.StatusCode = (int)HttpStatusCode.InternalServerError;
                return res;
            }
        }

        private async Task<ICacheServiceClient> GetCacheServiceClient(string key)
        {
            var partitionInformation = await GetPartitionInformationForCacheKey(key);

            var info = (Int64RangePartitionInformation)partitionInformation;
            var resolvedPartition = new ServicePartitionKey(info.LowKey);

            return new CacheServiceClient(
                _context,
                new CachePartitionClient(
                    new ServicePartitionClient<CacheCommunicationClient>(
                        _clientFactory,
                        _context.ServiceUri,
                        resolvedPartition)));
        }

        private async Task<ServicePartitionInformation> GetPartitionInformationForCacheKey(string cacheKey)
        {
            await InitializeAsync();

            var md5 = MD5.Create();
            var value = md5.ComputeHash(Encoding.ASCII.GetBytes(cacheKey));
            var key = BitConverter.ToInt64(value, 0);

            var partition = _servicePartitionList.Single(p => ((Int64RangePartitionInformation)p.PartitionInformation).LowKey <= key && ((Int64RangePartitionInformation)p.PartitionInformation).HighKey >= key);
            return partition.PartitionInformation;
        }

        private async Task InitializeAsync()
        {
            bool initPartitionList = false;

            if (InitializePartitionList() || RefreshPartitionList())
            {
                lock (_lock)
                {
                    if (InitializePartitionList())
                    {
                        ServiceEventSource.Current.ServiceMessage(_context, $"CacheController::InitializeAsync: Initialize");
                        _fabricClient = new FabricClient();
                        initPartitionList = true;
                    }

                    if (RefreshPartitionList())
                    {
                        ServiceEventSource.Current.ServiceMessage(_context, $"CacheController::InitializeAsync: Refresh {_lastInitializeTime}");
                        initPartitionList = true;
                    }
                }
            }

            if (initPartitionList && RefreshPartitionList())
            {
                // Note: there is a small chance that this gets executed multiple times when _servicePartitionList == null
                ServiceEventSource.Current.ServiceMessage(_context, $"CacheController::InitializeAsync: GetPartitionListAsync");
                _servicePartitionList = await _fabricClient.QueryManager.GetPartitionListAsync(_context.ServiceUri);
            }

            _lastInitializeTime = DateTime.UtcNow;
        }

        private bool InitializePartitionList()
        {
            return _fabricClient == null || _servicePartitionList == null;
        }

        private bool RefreshPartitionList()
        {
            return (DateTime.UtcNow - _lastInitializeTime) > TimeSpan.FromMinutes(10);
        }
    }
}
