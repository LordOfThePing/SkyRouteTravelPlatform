namespace SkyRoute.Domain.Entities;

public class Booking
{
    public int Id { get; set; }
    public string Reference { get; set; } = string.Empty;
    public string FlightId { get; set; } = string.Empty;
    public string OriginCode { get; set; } = string.Empty;
    public string DestinationCode { get; set; } = string.Empty;
    public string CabinClass { get; set; } = string.Empty;
    public decimal TotalPrice { get; set; }
    public string Currency { get; set; } = "USD";
    public DateTimeOffset CreatedAt { get; set; }
    public List<Passenger> Passengers { get; set; } = [];
}
