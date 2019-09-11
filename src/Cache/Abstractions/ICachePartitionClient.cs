namespace Cache.Abstractions
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using Cache.Client;

    public interface ICachePartitionClient
    {
        Task InvokeWithRetryAsync(
            Func<CacheCommunicationClient, Task> func,
            CancellationToken token,
            params Type[] doNotRetryExceptionTypes);

        Task<TResult> InvokeWithRetryAsync<TResult>(
            Func<CacheCommunicationClient, Task<TResult>> func,
            CancellationToken token,
            params Type[] doNotRetryExceptionTypes);
    }
}
