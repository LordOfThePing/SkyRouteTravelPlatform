using FluentValidation;
using SkyRoute.Api.DTOs;
using SkyRoute.Infrastructure.Data;

namespace SkyRoute.Api.Validators;

public class SearchRequestValidator : AbstractValidator<SearchRequestDto>
{
    private static readonly string[] AllowedCabins = ["Economy", "Business", "First Class"];

    public SearchRequestValidator()
    {
        RuleFor(x => x.OriginCode)
            .NotEmpty()
            .Length(3)
            .Matches("^[A-Z]{3}$").WithMessage("'{PropertyName}' must be a 3-letter IATA code.")
            .Must(AirportCatalog.Exists).WithMessage("Origin airport not found.");

        RuleFor(x => x.DestinationCode)
            .NotEmpty()
            .Length(3)
            .Matches("^[A-Z]{3}$").WithMessage("'{PropertyName}' must be a 3-letter IATA code.")
            .Must(AirportCatalog.Exists).WithMessage("Destination airport not found.");

        RuleFor(x => x)
            .Must(x => !string.Equals(x.OriginCode, x.DestinationCode, StringComparison.OrdinalIgnoreCase))
            .WithName("Route")
            .WithMessage("Origin and destination cannot be the same.");

        RuleFor(x => x.DepartureDate)
            .GreaterThanOrEqualTo(DateOnly.FromDateTime(DateTime.UtcNow.Date))
            .WithMessage("Departure date cannot be in the past.");

        RuleFor(x => x.Passengers)
            .InclusiveBetween(1, 9);

        RuleFor(x => x.CabinClass)
            .NotEmpty()
            .Must(c => AllowedCabins.Contains(c))
            .WithMessage("Cabin class must be Economy, Business, or First Class.");
    }
}
