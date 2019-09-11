namespace Cache
{
    using System;
    using System.Fabric;
    using Cache.Abstractions;

    public class StatefulContextWrapper : IStatefulContext
    {
        private StatefulServiceContext _context;

        public StatefulContextWrapper(StatefulServiceContext context)
        {
            _context = context;
        }

        public Uri  ServiceUri
        {
            get
            {
                return _context.ServiceName;
            }
        }

        public string NodeAddress
        {
            get
            {
                return _context.NodeContext.IPAddressOrFQDN;
            }
        }
    }
}
