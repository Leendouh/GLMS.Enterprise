namespace GLMS.Enterprise.Core.Interfaces;

public interface IExchangeRateApiService
{
    Task<decimal> GetUsdToZarRateAsync();
    Task<decimal> GetRateAsync(string fromCurrency, string toCurrency);
}
