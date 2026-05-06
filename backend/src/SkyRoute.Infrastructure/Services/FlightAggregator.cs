using Microsoft.Extensions.Logging;
using SkyRoute.Domain.Interfaces;
using SkyRoute.Domain.Models;

namespace SkyRoute.Infrastructure.Services;

public class FlightAggregator : IFlightAggregator
{
    public static readonly TimeSpan DefaultPerProviderTimeout = TimeSpan.FromSeconds(5);

    private readonly IEnumerable<IFlightProvider> _providers;
    private readonly ILogger<FlightAggregator> _logger;
    private readonly TimeSpan _perProviderTimeout;

    public FlightAggregator(
        IEnumerable<IFlightProvider> providers,
        ILogger<FlightAggregator> logger,
        TimeSpan? perProviderTimeout = null)
    {
        _providers = providers;
        _logger = logger;
        _perProviderTimeout = perProviderTimeout ?? DefaultPerProviderTimeout;
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
        // Per-provider timeout protects the aggregate response from one slow / hung
        // upstream. Caller's cancellation always wins; only the linked source cancels
        // on timeout.
        using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        timeoutCts.CancelAfter(_perProviderTimeout);

        try
        {
            return await provider.SearchAsync(request, timeoutCts.Token);
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            // Caller cancelled — propagate so the aggregate response cancels too.
            throw;
        }
        catch (OperationCanceledException) when (timeoutCts.IsCancellationRequested)
        {
            _logger.LogWarning(
                "Provider {ProviderId} timed out after {TimeoutMs}ms",
                provider.ProviderId, _perProviderTimeout.TotalMilliseconds);
            return [];
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Provider {ProviderId} failed during search", provider.ProviderId);
            return [];
        }
    }
}
