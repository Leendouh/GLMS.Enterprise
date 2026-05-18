using GLMS.Enterprise.Core.Interfaces;
using GLMS.Enterprise.Services.Currency;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace GLMS.Enterprise.Tests.Currency;

public class CurrencyStrategyTests
{
    [Fact]
    public async Task FixedRateStrategy_Convert_100USD_To_ZAR_Returns_1850()
    {
        // Arrange
        var strategy = new FixedRateStrategy();
        decimal amount = 100m;
        string fromCurrency = "USD";
        string toCurrency = "ZAR";

        // Act
        var result = await strategy.ConvertAsync(amount, fromCurrency, toCurrency);

        // Assert
        Assert.Equal(1850m, result);
    }

    [Fact]
    public async Task FixedRateStrategy_Convert_ZeroAmount_Returns_Zero()
    {
        // Arrange
        var strategy = new FixedRateStrategy();
        decimal amount = 0m;

        // Act
        var result = await strategy.ConvertAsync(amount, "USD", "ZAR");

        // Assert
        Assert.Equal(0m, result);
    }

    [Fact]
    public async Task FixedRateStrategy_Convert_NegativeAmount_Throws_ArgumentOutOfRangeException()
    {
        // Arrange
        var strategy = new FixedRateStrategy();
        decimal amount = -100m;

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() =>
            strategy.ConvertAsync(amount, "USD", "ZAR"));
    }

    [Fact]
    public async Task FixedRateStrategy_Convert_HighPrecision_Returns_CorrectValue()
    {
        // Arrange
        var strategy = new FixedRateStrategy();
        decimal amount = 99.99m;
        decimal expected = Math.Round(99.99m * 18.50m, 2);

        // Act
        var result = await strategy.ConvertAsync(amount, "USD", "ZAR");

        // Assert
        Assert.Equal(expected, result);
    }

    [Fact]
    public async Task FixedRateStrategy_Convert_SameCurrency_Returns_SameAmount()
    {
        // Arrange
        var strategy = new FixedRateStrategy();
        decimal amount = 100m;

        // Act
        var result = await strategy.ConvertAsync(amount, "USD", "USD");

        // Assert
        Assert.Equal(amount, result);
    }

    [Fact]
    public async Task FixedRateStrategy_Convert_NullFromCurrency_Throws_ArgumentException()
    {
        // Arrange
        var strategy = new FixedRateStrategy();

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() =>
            strategy.ConvertAsync(100m, null!, "ZAR"));
    }

    [Fact]
    public async Task FixedRateStrategy_Convert_EmptyToCurrency_Throws_ArgumentException()
    {
        // Arrange
        var strategy = new FixedRateStrategy();

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() =>
            strategy.ConvertAsync(100m, "USD", ""));
    }

    [Fact]
    public async Task FixedRateStrategy_Convert_UnsupportedCurrency_Throws_NotSupportedException()
    {
        // Arrange
        var strategy = new FixedRateStrategy();

        // Act & Assert
        await Assert.ThrowsAsync<NotSupportedException>(() =>
            strategy.ConvertAsync(100m, "JPY", "ZAR"));
    }

    [Fact]
    public async Task FixedRateStrategy_GetRate_USD_To_ZAR_Returns_18_50()
    {
        // Arrange
        var strategy = new FixedRateStrategy();

        // Act
        var rate = await strategy.GetRateAsync("USD", "ZAR");

        // Assert
        Assert.Equal(18.50m, rate);
    }

    [Fact]
    public async Task FixedRateStrategy_GetRate_ZAR_To_USD_Returns_Reciprocal()
    {
        // Arrange
        var strategy = new FixedRateStrategy();
        decimal expected = 1m / 18.50m;

        // Act
        var rate = await strategy.GetRateAsync("ZAR", "USD");

        // Assert
        Assert.Equal(expected, rate);
    }

    [Fact]
    public async Task FixedRateStrategy_Convert_EUR_To_ZAR_Returns_CorrectValue()
    {
        // Arrange
        var strategy = new FixedRateStrategy();
        decimal amount = 100m;
        decimal expected = Math.Round(100m * (18.50m / 0.93m), 2);

        // Act
        var result = await strategy.ConvertAsync(amount, "EUR", "ZAR");

        // Assert
        Assert.Equal(expected, result);
    }
}
