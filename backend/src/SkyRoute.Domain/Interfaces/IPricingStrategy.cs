namespace SkyRoute.Domain.Interfaces;

public interface IPricingStrategy
{
    decimal PriceFor(decimal baseFare);
}
