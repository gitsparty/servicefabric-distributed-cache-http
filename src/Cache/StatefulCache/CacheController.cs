using System;
using System.IO;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;

namespace Cache.StatefulCache
{
    [Route("api/[controller]")]
    [ApiController]
    public class CacheController : ControllerBase
    {
        private readonly ICache _cache;

        public CacheController(
            ICache cache)
        {
            _cache = cache;
        }

        [HttpGet("BaselinePerf")]
        public Task<ActionResult<string>> BaselinePerf(CancellationToken cancellationToken)
        {
            ActionResult<string> result = Content("nas");
            return Task.FromResult(result);
        }

        [HttpGet("GetAbsoluteExpirationCacheItem")]
        public async Task<ActionResult<string>> GetAbsoluteExpirationCacheItem(CancellationToken cancellationToken)
        {
            var bytes = await _cache.GetAsync("AbsoluteExpirationCacheItem", cancellationToken);

            if (bytes != null)
                return Content(Encoding.UTF8.GetString(bytes));

            return new EmptyResult();
        }

        [HttpGet("{key}")]
        public async Task<ActionResult<string>> Get(string key, CancellationToken cancellationToken)
        {
            try
            {
                var bytes = await _cache.GetAsync(key, cancellationToken);

                if (bytes != null)
                    return Content(Encoding.UTF8.GetString(bytes));

                return new NotFoundResult();
            }
            catch (Exception ex)
            {
                var res = new ObjectResult(ex);
                res.StatusCode = (int)HttpStatusCode.InternalServerError;
                return res;
            }
        }

        [HttpPut("{key}")]
        public async Task Put(string key, CancellationToken cancellationToken)
        {
            var request = HttpContext.Request;
            using (var reader = new StreamReader(request.Body))
            {
                var content = await reader.ReadToEndAsync();

                await _cache.SetAsync(key, Encoding.UTF8.GetBytes(content), cancellationToken);
            }
        }

        [HttpPost("{key}")]
        public async Task<ActionResult<string>> Post(string key, CancellationToken cancellationToken)
        {
            var request = HttpContext.Request;
            using (var reader = new StreamReader(request.Body))
            {
                var content = await reader.ReadToEndAsync();

                try
                {
                    var result = await _cache.CreateCachedItemAsync(
                        key,
                        Encoding.UTF8.GetBytes(content),
                        cancellationToken);

                    if (result == null)
                    {
                        return new ConflictResult();
                    }

                    return Created("Created", Encoding.UTF8.GetString(result));
                }
                catch (Exception ex)
                {
                    var res = new ObjectResult(ex);
                    res.StatusCode = (int)HttpStatusCode.InternalServerError;
                    return res;
                }
            }
        }

        [HttpDelete("{key}")]
        public async Task Delete(string key, CancellationToken cancellationToken)
        {
            await _cache.RemoveAsync(key, cancellationToken);
        }
    }
}
