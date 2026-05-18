using GLMS.Enterprise.Services.Currency;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace GLMS.Enterprise.Tests.Currency;

public class CachedCurrencyStrategyTests
{
    [Fact]
    public async Task CachedCurrencyStrategy_Convert_ZeroAmount_Returns_Zero()
    {
        // Arrange
        var mockInner = new Mock<LiveApiCurrencyStrategy>();
        var mockCache = new MemoryCache(new MemoryCacheOptions());
        var mockLogger = new Mock<ILogger<CachedCurrencyStrategy>>();
        var strategy = new CachedCurrencyStrategy(mockInner.Object, mockCache, mockLogger.Object);

        // Act
        var result = await strategy.ConvertAsync(0m, "USD", "ZAR");

        // Assert
        Assert.Equal(0m, result);
    }

    [Fact]
    public async Task CachedCurrencyStrategy_Convert_NegativeAmount_Throws_ArgumentOutOfRangeException()
    {
        // Arrange
        var mockInner = new Mock<LiveApiCurrencyStrategy>();
        var mockCache = new MemoryCache(new MemoryCacheOptions());
        var mockLogger = new Mock<ILogger<CachedCurrencyStrategy>>();
        var strategy = new CachedCurrencyStrategy(mockInner.Object, mockCache, mockLogger.Object);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() =>
            strategy.ConvertAsync(-100m, "USD", "ZAR"));
    }

    [Fact]
    public async Task CachedCurrencyStrategy_Convert_SameCurrency_Returns_SameAmount()
    {
        // Arrange
        var mockInner = new Mock<LiveApiCurrencyStrategy>();
        var mockCache = new MemoryCache(new MemoryCacheOptions());
        var mockLogger = new Mock<ILogger<CachedCurrencyStrategy>>();
        var strategy = new CachedCurrencyStrategy(mockInner.Object, mockCache, mockLogger.Object);
        decimal amount = 100m;

        // Act
        var result = await strategy.ConvertAsync(amount, "USD", "USD");

        // Assert
        Assert.Equal(amount, result);
    }

    [Fact]
    public async Task CachedCurrencyStrategy_CachesRate_SecondCall_UsesCache()
    {
        // Arrange
        var mockInner = new Mock<LiveApiCurrencyStrategy>();
        mockInner.Setup(x => x.GetRateAsync("USD", "ZAR"))
            .ReturnsAsync(18.50m)
            .Verifiable();

        var mockCache = new MemoryCache(new MemoryCacheOptions());
        var mockLogger = new Mock<ILogger<CachedCurrencyStrategy>>();
        var strategy = new CachedCurrencyStrategy(mockInner.Object, mockCache, mockLogger.Object);

        // Act - First call should hit the inner strategy
        var rate1 = await strategy.GetRateAsync("USD", "ZAR");
        var rate2 = await strategy.GetRateAsync("USD", "ZAR");

        // Assert
        Assert.Equal(18.50m, rate1);
        Assert.Equal(18.50m, rate2);
        mockInner.Verify(x => x.GetRateAsync("USD", "ZAR"), Times.Once); // Only called once due to cache
    }
}
