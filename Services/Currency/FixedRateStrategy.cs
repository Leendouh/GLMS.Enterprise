using GLMS.Enterprise.Core.Interfaces;

namespace GLMS.Enterprise.Services.Currency;

/// <summary>
/// Strategy 3 (FIXED): Uses hardcoded exchange rates as an emergency fallback.
/// No external calls — always available, but rates may be stale.
/// </summary>
public class FixedRateStrategy : ICurrencyStrategy
{
    // Hardcoded rates relative to USD
    private static readonly Dictionary<string, decimal> _ratesFromUsd = new(StringComparer.OrdinalIgnoreCase)
    {
        ["USD"] = 1.00m,
        ["ZAR"] = 18.50m,
        ["EUR"] = 0.93m,
        ["GBP"] = 0.79m,
        ["AUD"] = 1.56m,
    };

    public Task<decimal> GetRateAsync(string fromCurrency, string toCurrency)
    {
        if (string.IsNullOrWhiteSpace(fromCurrency))
            throw new ArgumentException("fromCurrency cannot be null or empty.", nameof(fromCurrency));
        if (string.IsNullOrWhiteSpace(toCurrency))
            throw new ArgumentException("toCurrency cannot be null or empty.", nameof(toCurrency));

        fromCurrency = fromCurrency.ToUpperInvariant();
        toCurrency   = toCurrency.ToUpperInvariant();

        if (fromCurrency == toCurrency) return Task.FromResult(1m);

        // Convert via USD as pivot currency
        if (!_ratesFromUsd.TryGetValue(fromCurrency, out var fromRate))
            throw new NotSupportedException($"Fixed rate not available for currency: {fromCurrency}");
        if (!_ratesFromUsd.TryGetValue(toCurrency, out var toRate))
            throw new NotSupportedException($"Fixed rate not available for currency: {toCurrency}");

        // rate = toRate / fromRate  (e.g. ZAR/USD = 18.50 / 1.00 = 18.50)
        var rate = toRate / fromRate;
        return Task.FromResult(rate);
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
