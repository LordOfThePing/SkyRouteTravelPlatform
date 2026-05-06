namespace SkyRoute.Domain.Models;

public record SearchRequest(
    string OriginCode,
    string DestinationCode,
    DateOnly DepartureDate,
    int Passengers,
    string CabinClass
);
