using SkyRoute.Domain.Models;

namespace SkyRoute.Domain.Interfaces;

public interface IFlightAggregator
{
    Task<IReadOnlyList<FlightOffer>> SearchAsync(SearchRequest request, CancellationToken ct = default);
}
