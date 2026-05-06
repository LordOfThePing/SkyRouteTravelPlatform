using Microsoft.Extensions.Logging;
using SkyRoute.Domain.Interfaces;
using SkyRoute.Domain.Models;

namespace SkyRoute.Infrastructure.Services;

public class FlightAggregator : IFlightAggregator
{
    private readonly IEnumerable<IFlightProvider> _providers;
    private readonly ILogger<FlightAggregator> _logger;

    public FlightAggregator(IEnumerable<IFlightProvider> providers, ILogger<FlightAggregator> logger)
    {
        _providers = providers;
        _logger = logger;
    }

    public async Task<IReadOnlyList<FlightOffer>> SearchAsync(SearchRequest request, CancellationToken ct = default)
    {
        var tasks = _providers.Select(p => SearchSafeAsync(p, request, ct));
        var results = await Task.WhenAll(tasks);
        return results.SelectMany(r => r).ToList();
    }

    private async Task<IReadOnlyList<FlightOffer>> SearchSafeAsync(
        IFlightProvider provider, SearchRequest request, CancellationToken ct)
    {
        try
        {
            return await provider.SearchAsync(request, ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Provider {ProviderId} failed during search", provider.ProviderId);
            return [];
        }
    }
}
