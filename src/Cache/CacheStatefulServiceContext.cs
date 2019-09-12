namespace Cache
{
    using System;
    using System.Fabric;
    using Cache.Abstractions;

    public class CacheStatefulServiceContext : IStatefulContext
    {
        private StatefulServiceContext _context;

        public CacheStatefulServiceContext(StatefulServiceContext context)
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

        public string ServiceTypeName
        {
            get
            {
                return _context.ServiceTypeName;
            }
        }

        public string ServiceName
        {
            get
            {
                return _context.ServiceName.ToString();
            }
        }

        public long ReplicaId
        {
            get
            {
                return _context.ReplicaId;
            }
        }

        public Guid PartitionId
        {
            get
            {
                return _context.PartitionId;
            }
        }

        public string ApplicationName
        {
            get
            {
                return _context.CodePackageActivationContext.ApplicationName;
            }
        }

        public string ApplicationTypeName
        {
            get
            {
                return _context.CodePackageActivationContext.ApplicationTypeName;
            }
        }

        public string NodeName
        {
            get
            {
                return _context.NodeContext.NodeName;
            }
        }

        public static implicit operator CacheStatefulServiceContext(StatefulServiceContext x)
        {
            return new CacheStatefulServiceContext(x);
        }
    }
}
