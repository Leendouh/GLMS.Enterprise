namespace GLMS.Enterprise.Core.Interfaces;

/// <summary>
/// Strategy interface for currency conversion.
/// Concrete strategies: LiveApiCurrencyStrategy, CachedCurrencyStrategy, FixedRateStrategy.
/// </summary>
public interface ICurrencyStrategy
{
    Task<decimal> ConvertAsync(decimal amount, string fromCurrency, string toCurrency);
    Task<decimal> GetRateAsync(string fromCurrency, string toCurrency);
}
