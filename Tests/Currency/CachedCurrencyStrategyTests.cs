using GLMS.Enterprise.Core.Interfaces;
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
        var mockInner = new Mock<ICurrencyStrategy>();
        var strategy = CreateStrategy(mockInner.Object);

        var result = await strategy.ConvertAsync(0m, "USD", "ZAR");

        Assert.Equal(0m, result);
        mockInner.Verify(x => x.GetRateAsync(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task CachedCurrencyStrategy_Convert_NegativeAmount_Throws_ArgumentOutOfRangeException()
    {
        var mockInner = new Mock<ICurrencyStrategy>();
        var strategy = CreateStrategy(mockInner.Object);

        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() =>
            strategy.ConvertAsync(-100m, "USD", "ZAR"));
    }

    [Fact]
    public async Task CachedCurrencyStrategy_Convert_SameCurrency_UsesInnerRate()
    {
        var mockInner = new Mock<ICurrencyStrategy>();
        mockInner.Setup(x => x.GetRateAsync("USD", "USD")).ReturnsAsync(1m);
        var strategy = CreateStrategy(mockInner.Object);

        var result = await strategy.ConvertAsync(100m, "USD", "USD");

        Assert.Equal(100m, result);
    }

    [Fact]
    public async Task CachedCurrencyStrategy_CachesRate_SecondCall_UsesCache()
    {
        var mockInner = new Mock<ICurrencyStrategy>();
        mockInner.Setup(x => x.GetRateAsync("USD", "ZAR"))
            .ReturnsAsync(18.50m);

        var strategy = CreateStrategy(mockInner.Object);

        var rate1 = await strategy.GetRateAsync("USD", "ZAR");
        var rate2 = await strategy.GetRateAsync("USD", "ZAR");

        Assert.Equal(18.50m, rate1);
        Assert.Equal(18.50m, rate2);
        mockInner.Verify(x => x.GetRateAsync("USD", "ZAR"), Times.Once);
    }

    private static CachedCurrencyStrategy CreateStrategy(ICurrencyStrategy inner)
    {
        var cache = new MemoryCache(new MemoryCacheOptions());
        var logger = new Mock<ILogger<CachedCurrencyStrategy>>();
        return new CachedCurrencyStrategy(inner, cache, logger.Object);
    }
}
