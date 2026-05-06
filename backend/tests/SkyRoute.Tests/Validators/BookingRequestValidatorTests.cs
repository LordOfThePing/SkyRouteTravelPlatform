using SkyRoute.Api.DTOs;
using SkyRoute.Api.Validators;

namespace SkyRoute.Tests.Validators;

public class BookingRequestValidatorTests
{
    private readonly BookingRequestValidator _validator = new();

    [Fact]
    public void Validate_WhenPassengerEmailInvalid_ReturnsInvalid()
    {
        var request = BuildValidRequest() with
        {
            Passengers = [new PassengerDto("Jane Doe", "invalid-email", "Passport", "A1234567")]
        };

        var result = _validator.Validate(request);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName.Contains("Email"));
    }

    [Fact]
    public void Validate_WhenDocumentNumberMissing_ReturnsInvalid()
    {
        var request = BuildValidRequest() with
        {
            Passengers = [new PassengerDto("Jane Doe", "jane@example.com", "Passport", string.Empty)]
        };

        var result = _validator.Validate(request);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName.Contains("DocumentNumber"));
    }

    [Fact]
    public void Validate_WhenFlightIdMissing_ReturnsInvalid()
    {
        var request = BuildValidRequest() with { FlightId = string.Empty };

        var result = _validator.Validate(request);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "FlightId");
    }

    private static BookingRequestDto BuildValidRequest() =>
        new(
            "FL-123",
            "MAD",
            "JFK",
            "Economy",
            120.5m,
            [new PassengerDto("Jane Doe", "jane@example.com", "Passport", "A1234567")]
        );
}
