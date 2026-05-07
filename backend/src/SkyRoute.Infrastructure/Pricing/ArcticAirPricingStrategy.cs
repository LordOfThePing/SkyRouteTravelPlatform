using SkyRoute.Domain.Interfaces;

namespace SkyRoute.Infrastructure.Pricing;

public class ArcticAirPricingStrategy : IPricingStrategy
{
    private const decimal LoyaltyDiscount = 10m;
    private const decimal Floor = 49.99m;

    public decimal PriceFor(decimal baseFare) =>
        Math.Max(Math.Round(baseFare * 1.20m - LoyaltyDiscount, 2), Floor);
}
