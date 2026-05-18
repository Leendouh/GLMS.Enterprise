using GLMS.Enterprise.Core.Interfaces;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace GLMS.Enterprise.Services.Currency;

/// <summary>
/// Strategy 1 (LIVE): Calls the ExchangeRate-API to get real-time rates.
/// Uses IHttpClientFactory for resilience. Falls back to 18.50 if API is unavailable.
/// </summary>
public class LiveApiCurrencyStrategy : ICurrencyStrategy
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<LiveApiCurrencyStrategy> _logger;
    private const decimal FallbackUsdToZar = 18.50m;

    public LiveApiCurrencyStrategy(IHttpClientFactory httpClientFactory,
                                   ILogger<LiveApiCurrencyStrategy> logger)
    {
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    public async Task<decimal> GetRateAsync(string fromCurrency, string toCurrency)
    {
        if (string.IsNullOrWhiteSpace(fromCurrency))
            throw new ArgumentException("fromCurrency cannot be null or empty.", nameof(fromCurrency));
        if (string.IsNullOrWhiteSpace(toCurrency))
            throw new ArgumentException("toCurrency cannot be null or empty.", nameof(toCurrency));

        fromCurrency = fromCurrency.ToUpperInvariant();
        toCurrency   = toCurrency.ToUpperInvariant();

        if (fromCurrency == toCurrency)
            return 1m;

        try
        {
            var client = _httpClientFactory.CreateClient("ExchangeRateApi");
            var url    = $"https://api.exchangerate-api.com/v4/latest/{fromCurrency}";

            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));
            var response = await client.GetAsync(url, cts.Token);
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(json);

            if (doc.RootElement.TryGetProperty("rates", out var rates) &&
                rates.TryGetProperty(toCurrency, out var rateElement))
            {
                return rateElement.GetDecimal();
            }

            _logger.LogWarning("Rate for {To} not found in API response. Using fallback.", toCurrency);
            return GetFallbackRate(fromCurrency, toCurrency);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to fetch exchange rate {From}→{To}. Using fallback.", fromCurrency, toCurrency);
            return GetFallbackRate(fromCurrency, toCurrency);
        }
    }

    public async Task<decimal> ConvertAsync(decimal amount, string fromCurrency, string toCurrency)
    {
        if (amount < 0)
            throw new ArgumentOutOfRangeException(nameof(amount), "Amount cannot be negative.");

        if (amount == 0) return 0m;

        var rate = await GetRateAsync(fromCurrency, toCurrency);
        return Math.Round(amount * rate, 2);
    }

    private static decimal GetFallbackRate(string from, string to) =>
        (from, to) switch
        {
            ("USD", "ZAR") => FallbackUsdToZar,
            ("ZAR", "USD") => 1m / FallbackUsdToZar,
            _              => 1m
        };
}
