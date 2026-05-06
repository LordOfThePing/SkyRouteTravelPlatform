using SkyRoute.Infrastructure.Pricing;

namespace SkyRoute.Tests.Pricing;

public class BudgetWingsPricingStrategyTests
{
    private readonly BudgetWingsPricingStrategy _strategy = new();

    [Fact]
    public void PriceFor_WhenBaseFareIs100_Returns90()
    {
        var result = _strategy.PriceFor(100m);

        result.Should().Be(90.00m);
    }

    [Fact]
    public void PriceFor_WhenBaseFareHitsFloor_ReturnsFloor()
    {
        var result = _strategy.PriceFor(30m);

        result.Should().Be(29.99m);
    }

    [Fact]
    public void PriceFor_WhenBaseFareIs1000_Returns900()
    {
        var result = _strategy.PriceFor(1000m);

        result.Should().Be(900.00m);
    }
}
