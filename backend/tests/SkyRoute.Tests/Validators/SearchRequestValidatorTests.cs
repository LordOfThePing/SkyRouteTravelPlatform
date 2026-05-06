using SkyRoute.Api.DTOs;
using SkyRoute.Api.Validators;

namespace SkyRoute.Tests.Validators;

public class SearchRequestValidatorTests
{
    private readonly SearchRequestValidator _validator = new();

    [Theory]
    [InlineData(0)]
    [InlineData(10)]
    public void Validate_WhenPassengersOutOfRange_ReturnsInvalid(int passengers)
    {
        var request = BuildValidRequest() with { Passengers = passengers };

        var result = _validator.Validate(request);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Passengers");
    }

    [Fact]
    public void Validate_WhenOriginEqualsDestination_ReturnsInvalid()
    {
        var request = BuildValidRequest() with { DestinationCode = "MAD" };

        var result = _validator.Validate(request);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Route");
    }

    [Fact]
    public void Validate_WhenDepartureDateInPast_ReturnsInvalid()
    {
        var request = BuildValidRequest() with { DepartureDate = DateOnly.FromDateTime(DateTime.UtcNow.Date.AddDays(-1)) };

        var result = _validator.Validate(request);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "DepartureDate");
    }

    private static SearchRequestDto BuildValidRequest() =>
        new("MAD", "BCN", DateOnly.FromDateTime(DateTime.UtcNow.Date.AddDays(1)), 1, "Economy");
}
