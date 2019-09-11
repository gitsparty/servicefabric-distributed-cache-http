namespace Cache
{
    using System.Threading;
    using System.Threading.Tasks;

    public interface ICache
    {
        Task<byte[]> GetAsync(
            string key,
            CancellationToken token = default(CancellationToken));

        Task RefreshAsync(
            string key,
            CancellationToken token = default(CancellationToken));

        Task RemoveAsync(
            string key,
            CancellationToken token = default(CancellationToken));

        Task SetAsync(
            string key, byte[] value,
            CancellationToken token = default(CancellationToken));

        Task<byte[]> CreateCachedItemAsync(
            string key,
            byte[] value,
            CancellationToken token);
    }
}
