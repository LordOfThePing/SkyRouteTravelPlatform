namespace SkyRoute.Api.DTOs;

public record FlightOfferDto(
    string Id,
    string Provider,
    string FlightNumber,
    string OriginCode,
    string DestinationCode,
    DateTimeOffset DepartureTime,
    DateTimeOffset ArrivalTime,
    int DurationMinutes,
    string CabinClass,
    decimal PricePerPassenger,
    decimal TotalPrice,
    int Passengers,
    string Currency
);

public record SearchResponseDto(IReadOnlyList<FlightOfferDto> Results);
