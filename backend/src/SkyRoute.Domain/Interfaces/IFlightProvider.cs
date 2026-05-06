using SkyRoute.Domain.Models;

namespace SkyRoute.Domain.Interfaces;

public interface IFlightProvider
{
    string ProviderId { get; }
    Task<IReadOnlyList<FlightOffer>> SearchAsync(SearchRequest request, CancellationToken ct = default);
}
