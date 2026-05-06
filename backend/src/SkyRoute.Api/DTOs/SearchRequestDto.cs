namespace SkyRoute.Api.DTOs;

public record SearchRequestDto(
    string OriginCode,
    string DestinationCode,
    DateOnly DepartureDate,
    int Passengers,
    string CabinClass
);
