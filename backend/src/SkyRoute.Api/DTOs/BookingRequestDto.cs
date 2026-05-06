namespace SkyRoute.Api.DTOs;

public record BookingRequestDto(
    string FlightId,
    string OriginCode,
    string DestinationCode,
    string CabinClass,
    decimal TotalPrice,
    IReadOnlyList<PassengerDto> Passengers
);

public record PassengerDto(
    string FullName,
    string Email,
    string DocumentType,
    string DocumentNumber
);

public record BookingResponseDto(
    string BookingReference,
    DateTimeOffset CreatedAt,
    int PassengerCount
);
