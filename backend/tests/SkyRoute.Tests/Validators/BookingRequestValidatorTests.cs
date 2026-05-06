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

    [Theory]
    [InlineData("AB12")]              // too short
    [InlineData("ABCDEFGHIJKLM")]     // 13 chars, too long for passport
    [InlineData("AB-12345")]          // dash not allowed
    [InlineData("AB 12345")]          // space not allowed
    public void Validate_WhenPassportFormatInvalid_ReturnsInvalid(string documentNumber)
    {
        var request = BuildValidRequest() with
        {
            Passengers = [new PassengerDto("Jane Doe", "jane@example.com", "Passport", documentNumber)]
        };

        var result = _validator.Validate(request);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName.Contains("DocumentNumber"));
    }

    [Theory]
    [InlineData("12345A")]            // 6 chars — minimum passport
    [InlineData("X1234567")]          // typical passport
    [InlineData("ABCDEF123456")]      // 12 chars — max passport
    public void Validate_WhenPassportFormatValid_ReturnsValid(string documentNumber)
    {
        var request = BuildValidRequest() with
        {
            Passengers = [new PassengerDto("Jane Doe", "jane@example.com", "Passport", documentNumber)]
        };

        var result = _validator.Validate(request);

        result.IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData("AB123")]                 // 5 chars, too short
    [InlineData("ABCDEFGHIJKLMNO")]       // 15 chars, too long
    [InlineData("12345678/A")]            // slash not allowed
    public void Validate_WhenNationalIdFormatInvalid_ReturnsInvalid(string documentNumber)
    {
        var request = BuildValidRequest() with
        {
            Passengers = [new PassengerDto("Jane Doe", "jane@example.com", "NationalId", documentNumber)]
        };

        var result = _validator.Validate(request);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName.Contains("DocumentNumber"));
    }

    [Theory]
    [InlineData("12345678A")]             // typical Spanish DNI
    [InlineData("ABCDEF")]                // 6 chars — minimum
    [InlineData("ABCDEFGHIJKLMN")]        // 14 chars — max
    public void Validate_WhenNationalIdFormatValid_ReturnsValid(string documentNumber)
    {
        var request = BuildValidRequest() with
        {
            Passengers = [new PassengerDto("Jane Doe", "jane@example.com", "NationalId", documentNumber)]
        };

        var result = _validator.Validate(request);

        result.IsValid.Should().BeTrue();
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
