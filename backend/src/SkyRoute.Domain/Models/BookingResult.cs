namespace SkyRoute.Domain.Models;

public record BookingResult(
    string BookingReference,
    DateTimeOffset CreatedAt,
    int PassengerCount
);
