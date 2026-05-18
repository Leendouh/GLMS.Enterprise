using GLMS.Enterprise.Core.Interfaces;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace GLMS.Enterprise.Services.Currency;

/// <summary>
/// Strategy 2 (CACHED): Wraps the live API strategy with a 15-minute in-memory cache.
/// Reduces API calls and improves resilience if the API goes down within the TTL window.
/// </summary>
public class CachedCurrencyStrategy : ICurrencyStrategy
{
    private readonly LiveApiCurrencyStrategy _inner;
    private readonly IMemoryCache _cache;
    private readonly ILogger<CachedCurrencyStrategy> _logger;
    private static readonly TimeSpan CacheTtl = TimeSpan.FromMinutes(15);

    public CachedCurrencyStrategy(LiveApiCurrencyStrategy inner,
                                   IMemoryCache cache,
                                   ILogger<CachedCurrencyStrategy> logger)
    {
        _inner  = inner;
        _cache  = cache;
        _logger = logger;
    }

    public async Task<decimal> GetRateAsync(string fromCurrency, string toCurrency)
    {
        var cacheKey = $"rate_{fromCurrency?.ToUpperInvariant()}_{toCurrency?.ToUpperInvariant()}";

        if (_cache.TryGetValue(cacheKey, out decimal cachedRate))
        {
            _logger.LogDebug("Cache HIT for {Key}", cacheKey);
            return cachedRate;
        }

        _logger.LogDebug("Cache MISS for {Key} — fetching from API", cacheKey);
        var rate = await _inner.GetRateAsync(fromCurrency!, toCurrency!);

        _cache.Set(cacheKey, rate, CacheTtl);
        return rate;
    }

    public async Task<decimal> ConvertAsync(decimal amount, string fromCurrency, string toCurrency)
    {
        if (amount < 0)
            throw new ArgumentOutOfRangeException(nameof(amount), "Amount cannot be negative.");
        if (amount == 0) return 0m;

        var rate = await GetRateAsync(fromCurrency, toCurrency);
        return Math.Round(amount * rate, 2);
    }
}
