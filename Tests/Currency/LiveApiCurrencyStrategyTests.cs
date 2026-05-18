using GLMS.Enterprise.Services.Currency;
using Microsoft.Extensions.Logging;
using Moq;
using System.Net;
using System.Net.Http;
using Xunit;

namespace GLMS.Enterprise.Tests.Currency;

public class LiveApiCurrencyStrategyTests
{
    [Fact]
    public async Task LiveApiCurrencyStrategy_Convert_ZeroAmount_Returns_Zero()
    {
        // Arrange
        var mockHttpClientFactory = new Mock<IHttpClientFactory>();
        var mockLogger = new Mock<ILogger<LiveApiCurrencyStrategy>>();
        var strategy = new LiveApiCurrencyStrategy(mockHttpClientFactory.Object, mockLogger.Object);

        // Act
        var result = await strategy.ConvertAsync(0m, "USD", "ZAR");

        // Assert
        Assert.Equal(0m, result);
    }

    [Fact]
    public async Task LiveApiCurrencyStrategy_Convert_NegativeAmount_Throws_ArgumentOutOfRangeException()
    {
        // Arrange
        var mockHttpClientFactory = new Mock<IHttpClientFactory>();
        var mockLogger = new Mock<ILogger<LiveApiCurrencyStrategy>>();
        var strategy = new LiveApiCurrencyStrategy(mockHttpClientFactory.Object, mockLogger.Object);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() =>
            strategy.ConvertAsync(-100m, "USD", "ZAR"));
    }

    [Fact]
    public async Task LiveApiCurrencyStrategy_Convert_NullFromCurrency_Throws_ArgumentException()
    {
        // Arrange
        var mockHttpClientFactory = new Mock<IHttpClientFactory>();
        var mockLogger = new Mock<ILogger<LiveApiCurrencyStrategy>>();
        var strategy = new LiveApiCurrencyStrategy(mockHttpClientFactory.Object, mockLogger.Object);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() =>
            strategy.ConvertAsync(100m, null!, "ZAR"));
    }

    [Fact]
    public async Task LiveApiCurrencyStrategy_Convert_EmptyToCurrency_Throws_ArgumentException()
    {
        // Arrange
        var mockHttpClientFactory = new Mock<IHttpClientFactory>();
        var mockLogger = new Mock<ILogger<LiveApiCurrencyStrategy>>();
        var strategy = new LiveApiCurrencyStrategy(mockHttpClientFactory.Object, mockLogger.Object);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() =>
            strategy.ConvertAsync(100m, "USD", ""));
    }

    [Fact]
    public async Task LiveApiCurrencyStrategy_Convert_SameCurrency_Returns_SameAmount()
    {
        // Arrange
        var mockHttpClientFactory = new Mock<IHttpClientFactory>();
        var mockLogger = new Mock<ILogger<LiveApiCurrencyStrategy>>();
        var strategy = new LiveApiCurrencyStrategy(mockHttpClientFactory.Object, mockLogger.Object);
        decimal amount = 100m;

        // Act
        var result = await strategy.ConvertAsync(amount, "USD", "USD");

        // Assert
        Assert.Equal(amount, result);
    }

    [Fact]
    public async Task LiveApiCurrencyStrategy_GetRate_NullFromCurrency_Throws_ArgumentException()
    {
        // Arrange
        var mockHttpClientFactory = new Mock<IHttpClientFactory>();
        var mockLogger = new Mock<ILogger<LiveApiCurrencyStrategy>>();
        var strategy = new LiveApiCurrencyStrategy(mockHttpClientFactory.Object, mockLogger.Object);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() =>
            strategy.GetRateAsync(null!, "ZAR"));
    }

    [Fact]
    public async Task LiveApiCurrencyStrategy_GetRate_EmptyToCurrency_Throws_ArgumentException()
    {
        // Arrange
        var mockHttpClientFactory = new Mock<IHttpClientFactory>();
        var mockLogger = new Mock<ILogger<LiveApiCurrencyStrategy>>();
        var strategy = new LiveApiCurrencyStrategy(mockHttpClientFactory.Object, mockLogger.Object);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() =>
            strategy.GetRateAsync("USD", ""));
    }
}
