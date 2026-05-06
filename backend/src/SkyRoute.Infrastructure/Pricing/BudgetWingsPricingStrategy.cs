using SkyRoute.Domain.Interfaces;

namespace SkyRoute.Infrastructure.Pricing;

public class BudgetWingsPricingStrategy : IPricingStrategy
{
    private const decimal Floor = 29.99m;

    public decimal PriceFor(decimal baseFare) =>
        Math.Max(Math.Round(baseFare * 0.90m, 2), Floor);
}
