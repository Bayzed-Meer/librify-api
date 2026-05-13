using Librify.Application.Auth.Interfaces;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace Librify.Infrastructure.Auth;

public class LoginAttemptTracker(IMemoryCache cache, ILogger<LoginAttemptTracker> logger) : ILoginAttemptTracker
{
    private const int FailureThreshold = 10;
    private static readonly TimeSpan Window = TimeSpan.FromMinutes(15);

    public bool IsBlocked(string email)
    {
        cache.TryGetValue(CacheKey(email), out int failures);
        return failures >= FailureThreshold;
    }

    public void RecordFailure(string email)
    {
        var key = CacheKey(email);
        var failures = cache.GetOrCreate(key, entry =>
        {
            entry.SlidingExpiration = Window;
            return 0;
        });

        failures++;
        cache.Set(key, failures, new MemoryCacheEntryOptions { SlidingExpiration = Window });

        logger.LogWarning("Failed login attempt #{Count} for {Email}", failures, email);
    }

    public void Reset(string email)
    {
        cache.Remove(CacheKey(email));
    }

    private static string CacheKey(string email) => $"login_failures:{email}";
}
