namespace Cache.Abstractions
{
    using System;

    public interface IStatefulContext
    {
        Uri ServiceUri { get; }

        string NodeAddress { get; }

        string ServiceTypeName { get; }

        string ServiceName { get; }

        long ReplicaId { get; }

        Guid PartitionId { get; }

        string ApplicationName { get; }

        string ApplicationTypeName { get; }

        string NodeName { get; }
    }
}
