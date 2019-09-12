using System;
using System.Collections.Generic;
using System.Text;

namespace Cache.Abstractions
{
    public interface IRequestContext
    {
        ICacheSvcStateContext StatefulServiceContext { get; }

        string CorrelationId { get; }
    }
}
