namespace Cache.Abstractions
{
    using System;

    public interface IStatefulContext
    {
        Uri ServiceUri { get; }

        string NodeAddress { get; }
    }
}
