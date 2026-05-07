using SkyRoute.Domain.Interfaces;
using SkyRoute.Domain.Models;
using SkyRoute.Infrastructure.Pricing;

namespace SkyRoute.Infrastructure.Providers;

public class ArcticAirProvider : IFlightProvider
{
    private readonly IPricingStrategy _pricing;

    public ArcticAirProvider(ArcticAirPricingStrategy pricing) => _pricing = pricing;

    public string ProviderId => "ArcticAir";

    public Task<IReadOnlyList<FlightOffer>> SearchAsync(SearchRequest request, CancellationToken ct = default)
    {
        var seed = HashCode.Combine(
            request.OriginCode.ToUpperInvariant(),
            request.DestinationCode.ToUpperInvariant(),
            request.DepartureDate.ToString("yyyy-MM-dd"),
            request.CabinClass,
            ProviderId);

        var rng = new Random(seed);
        var count = rng.Next(0, 6);
        var offers = new List<FlightOffer>(count);

        for (var i = 0; i < count; i++)
        {
            var departureHour = rng.Next(5, 23);
            var departureMinute = rng.Next(0, 12) * 5;
            var durationMinutes = rng.Next(45, 720);
            var baseFare = Math.Round((decimal)(rng.Next(35, 800) + rng.NextDouble()), 2);

            var departure = new DateTimeOffset(
                request.DepartureDate.Year, request.DepartureDate.Month, request.DepartureDate.Day,
                departureHour, departureMinute, 0, TimeSpan.Zero);

            var pricePerPassenger = _pricing.PriceFor(baseFare);
            var total = Math.Round(pricePerPassenger * request.Passengers, 2);
            var flightNum = $"AA{500 + Math.Abs(seed % 4000) + i}";

            offers.Add(new FlightOffer(
                Id: $"AA-{Math.Abs(seed):X6}-{i}",
                Provider: ProviderId,
                FlightNumber: flightNum,
                OriginCode: request.OriginCode.ToUpperInvariant(),
                DestinationCode: request.DestinationCode.ToUpperInvariant(),
                DepartureTime: departure,
                ArrivalTime: departure.AddMinutes(durationMinutes),
                DurationMinutes: durationMinutes,
                CabinClass: request.CabinClass,
                PricePerPassenger: pricePerPassenger,
                TotalPrice: total,
                Passengers: request.Passengers));
        }

        return Task.FromResult<IReadOnlyList<FlightOffer>>(offers);
    }
}
