using FluentValidation;
using SkyRoute.Api.DTOs;
using SkyRoute.Infrastructure.Data;

namespace SkyRoute.Api.Validators;

public class BookingRequestValidator : AbstractValidator<BookingRequestDto>
{
    private static readonly string[] AllowedDocTypes = ["Passport", "NationalId"];
    private static readonly string[] AllowedCabins = ["Economy", "Business", "First Class"];

    public BookingRequestValidator()
    {
        RuleFor(x => x.FlightId).NotEmpty().MaximumLength(50);

        RuleFor(x => x.OriginCode)
            .NotEmpty().Length(3)
            .Must(AirportCatalog.Exists).WithMessage("Origin airport not found.");

        RuleFor(x => x.DestinationCode)
            .NotEmpty().Length(3)
            .Must(AirportCatalog.Exists).WithMessage("Destination airport not found.");

        RuleFor(x => x.CabinClass)
            .Must(c => AllowedCabins.Contains(c))
            .WithMessage("Invalid cabin class.");

        RuleFor(x => x.TotalPrice).GreaterThan(0);

        RuleFor(x => x.Passengers).NotEmpty()
            .WithMessage("At least one passenger is required.");

        RuleForEach(x => x.Passengers).SetValidator(new PassengerValidator());
    }
}

public class PassengerValidator : AbstractValidator<PassengerDto>
{
    public PassengerValidator()
    {
        RuleFor(x => x.FullName).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Email).NotEmpty().MaximumLength(254).EmailAddress();
        RuleFor(x => x.DocumentType)
            .NotEmpty()
            .Must(t => t is "Passport" or "NationalId")
            .WithMessage("Document type must be Passport or NationalId.");
        RuleFor(x => x.DocumentNumber).NotEmpty().MaximumLength(32);
    }
}
