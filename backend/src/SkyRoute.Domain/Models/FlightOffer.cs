namespace SkyRoute.Domain.Models;

public record FlightOffer(
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
    string Currency = "USD"
);
