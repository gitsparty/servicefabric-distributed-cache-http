using System;
using System.IO;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Cache.Abstractions;
using Cache.Client;
using Microsoft.AspNetCore.Mvc;
using Microsoft.ServiceFabric.Services.Communication.Client;

namespace Cache.StatefulCache
{
    [Route("api/[controller]")]
    [ApiController]
    public class CacheController : ControllerBase
    {
        private readonly ICacheServiceClient _cacheClient;

        public CacheController(ICacheServiceClient client)
        {
            _cacheClient = client;
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
            try
            {
                var bytes = await _cacheClient.GetAsync(key, cancellationToken);

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

                await _cacheClient.SetAsync(key, Encoding.UTF8.GetBytes(content), cancellationToken);
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
                    var result = await _cacheClient.CreateCachedItemAsync(key, Encoding.UTF8.GetBytes(content), cancellationToken);

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
        public async Task<ActionResult<string>> Delete(string key, CancellationToken cancellationToken)
        {
            try
            {
                await _cacheClient.RemoveAsync(key, cancellationToken);

                var res = new ObjectResult("Deleted");
                res.StatusCode = (int)HttpStatusCode.OK;
                return res;
            }
            catch (Exception ex)
            {
                var res = new ObjectResult(ex);
                res.StatusCode = (int)HttpStatusCode.InternalServerError;
                return res;
            }
        }
    }
}
