namespace SkyRoute.Domain.Models;

public record BookingRequest(
    string FlightId,
    string OriginCode,
    string DestinationCode,
    string CabinClass,
    decimal TotalPrice,
    IReadOnlyList<PassengerRequest> Passengers
);

public record PassengerRequest(
    string FullName,
    string Email,
    string DocumentType,
    string DocumentNumber
);
