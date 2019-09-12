namespace Cache
{
    using System;
    using Cache.Abstractions;

    public class RequestContext : IRequestContext
    {
        public RequestContext(ICacheSvcStateContext context)
        {
            this.StatefulServiceContext = context;
            this.CorrelationId = Guid.NewGuid().ToString();
        }

        public ICacheSvcStateContext StatefulServiceContext { get; private set; }

        public string CorrelationId { get; private set; }
    }
}
