namespace Common.Caching;

using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Distributed;

public static class DistributedCacheExtensions
{
    public static async Task<T?> GetAsync<T>(this IDistributedCache cache, string key, CancellationToken cancellationToken = default)
        where T : class
    {
        return CacheHelper.FromByteArray<T>((await cache.GetAsync(key, cancellationToken)) !);
    }

    public static Task SetObjectAsync<T>(this IDistributedCache cache, string key, T value, CancellationToken cancellationToken = default)
    {
        if (value is null)
        {
            throw new ArgumentNullException(nameof(value));
        }

        return cache.SetAsync(key, CacheHelper.ToByteArray(value), cancellationToken);
    }

    public static Task SetObjectAsync<T>(this IDistributedCache cache, string key, T value, DistributedCacheEntryOptions options, CancellationToken cancellationToken = default)
    {
        if (value is null)
        {
            throw new ArgumentNullException(nameof(value));
        }

        return cache.SetAsync(key, CacheHelper.ToByteArray(value), options, cancellationToken);
    }
}
