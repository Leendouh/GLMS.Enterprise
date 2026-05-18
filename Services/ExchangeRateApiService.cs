using GLMS.Enterprise.Core.Interfaces;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace GLMS.Enterprise.Services;

/// <summary>
/// Dedicated service for fetching exchange rates from the ExchangeRate-API.
/// Uses IHttpClientFactory, 30-second timeout, and graceful fallback.
/// Registered separately from the Strategy pattern so the Controller
/// can call it independently for real-time JS updates.
/// </summary>
public class ExchangeRateApiService : IExchangeRateApiService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<ExchangeRateApiService> _logger;
    private const decimal FallbackUsdToZar = 18.50m;
    private const string BaseUrl = "https://api.exchangerate-api.com/v4/latest/";

    public ExchangeRateApiService(IHttpClientFactory httpClientFactory,
                                   ILogger<ExchangeRateApiService> logger)
    {
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    public async Task<decimal> GetUsdToZarRateAsync()
        => await GetRateAsync("USD", "ZAR");

    public async Task<decimal> GetRateAsync(string fromCurrency, string toCurrency)
    {
        fromCurrency = (fromCurrency ?? "USD").ToUpperInvariant();
        toCurrency   = (toCurrency   ?? "ZAR").ToUpperInvariant();

        // Retry up to 3 times with exponential back-off (T5).
        for (int attempt = 1; attempt <= 3; attempt++)
        {
            try
            {
                var client = _httpClientFactory.CreateClient("ExchangeRateApi");
                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));

                var response = await client.GetAsync($"{BaseUrl}{fromCurrency}", cts.Token);
                response.EnsureSuccessStatusCode();

                var json = await response.Content.ReadAsStringAsync();
                using var doc = JsonDocument.Parse(json);

                if (doc.RootElement.TryGetProperty("rates", out var rates) &&
                    rates.TryGetProperty(toCurrency, out var rateEl))
                {
                    return rateEl.GetDecimal();
                }

                _logger.LogWarning("Rate for {To} not found in API response.", toCurrency);
                return FallbackUsdToZar;
            }
            catch (TaskCanceledException)
            {
                _logger.LogWarning("ExchangeRate-API timed out (attempt {Attempt}/3).", attempt);
            }
            catch (HttpRequestException ex)
            {
                _logger.LogWarning(ex, "ExchangeRate-API request failed (attempt {Attempt}/3).", attempt);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error fetching exchange rate.");
                break;
            }

            if (attempt < 3)
                await Task.Delay(TimeSpan.FromSeconds(Math.Pow(2, attempt)));
        }

        _logger.LogWarning("All retry attempts failed. Returning fallback rate {Rate}.", FallbackUsdToZar);
        return FallbackUsdToZar;
    }
}
