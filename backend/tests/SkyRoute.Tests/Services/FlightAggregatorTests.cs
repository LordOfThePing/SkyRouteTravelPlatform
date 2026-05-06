using Microsoft.Extensions.Logging.Abstractions;
using SkyRoute.Domain.Interfaces;
using SkyRoute.Domain.Models;
using SkyRoute.Infrastructure.Services;

namespace SkyRoute.Tests.Services;

public class FlightAggregatorTests
{
    [Fact]
    public async Task SearchAsync_ReturnsUnionOfProviderResults()
    {
        var request = BuildRequest();
        var providerAOffer = BuildOffer("A-1", "ProviderA");
        var providerBOffer = BuildOffer("B-1", "ProviderB");
        var providers = new IFlightProvider[]
        {
            new StubFlightProvider("A", [providerAOffer]),
            new StubFlightProvider("B", [providerBOffer]),
        };
        var sut = new FlightAggregator(providers, NullLogger<FlightAggregator>.Instance);

        var result = await sut.SearchAsync(request);

        result.Should().HaveCount(2);
        result.Should().Contain(providerAOffer);
        result.Should().Contain(providerBOffer);
    }

    [Fact]
    public async Task SearchAsync_WhenOneProviderFails_StillReturnsOtherResults()
    {
        var request = BuildRequest();
        var healthyOffer = BuildOffer("OK-1", "ProviderHealthy");
        var providers = new IFlightProvider[]
        {
            new StubFlightProvider("healthy", [healthyOffer]),
            new ThrowingFlightProvider("failing"),
        };
        var sut = new FlightAggregator(providers, NullLogger<FlightAggregator>.Instance);

        var result = await sut.SearchAsync(request);

        result.Should().ContainSingle();
        result[0].Should().Be(healthyOffer);
    }

    private static SearchRequest BuildRequest() =>
        new("MAD", "JFK", new DateOnly(2026, 5, 10), 1, "Economy");

    private static FlightOffer BuildOffer(string id, string provider) =>
        new(
            id,
            provider,
            "SR1234",
            "MAD",
            "JFK",
            DateTimeOffset.Parse("2026-05-10T08:00:00Z"),
            DateTimeOffset.Parse("2026-05-10T12:00:00Z"),
            240,
            "Economy",
            100m,
            100m,
            1,
            "USD"
        );

    private sealed class StubFlightProvider(string providerId, IReadOnlyList<FlightOffer> offers) : IFlightProvider
    {
        public string ProviderId { get; } = providerId;

        public Task<IReadOnlyList<FlightOffer>> SearchAsync(SearchRequest request, CancellationToken ct = default) =>
            Task.FromResult(offers);
    }

    private sealed class ThrowingFlightProvider(string providerId) : IFlightProvider
    {
        public string ProviderId { get; } = providerId;

        public Task<IReadOnlyList<FlightOffer>> SearchAsync(SearchRequest request, CancellationToken ct = default) =>
            throw new InvalidOperationException("Simulated provider failure.");
    }
}
