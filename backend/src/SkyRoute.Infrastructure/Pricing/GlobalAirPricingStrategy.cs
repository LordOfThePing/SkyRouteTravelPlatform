using SkyRoute.Domain.Interfaces;

namespace SkyRoute.Infrastructure.Pricing;

public class GlobalAirPricingStrategy : IPricingStrategy
{
    public decimal PriceFor(decimal baseFare) => Math.Round(baseFare * 1.15m, 2);
}
