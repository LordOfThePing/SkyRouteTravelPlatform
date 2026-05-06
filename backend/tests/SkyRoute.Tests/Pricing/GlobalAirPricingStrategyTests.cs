using SkyRoute.Infrastructure.Pricing;

namespace SkyRoute.Tests.Pricing;

public class GlobalAirPricingStrategyTests
{
    private readonly GlobalAirPricingStrategy _strategy = new();

    [Fact]
    public void PriceFor_WhenBaseFareIs100_Returns115()
    {
        var result = _strategy.PriceFor(100m);

        result.Should().Be(115.00m);
    }

    [Fact]
    public void PriceFor_WhenBaseFareNeedsRounding_RoundsToTwoDecimals()
    {
        var result = _strategy.PriceFor(100.001m);

        result.Should().Be(115.00m);
    }

    [Fact]
    public void PriceFor_WhenBaseFareIsZero_ReturnsZero()
    {
        var result = _strategy.PriceFor(0m);

        result.Should().Be(0m);
    }
}
