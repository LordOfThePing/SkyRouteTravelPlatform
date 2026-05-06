using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using SkyRoute.Api.DTOs;
using SkyRoute.Domain.Entities;
using SkyRoute.Domain.Interfaces;

namespace SkyRoute.Api.Controllers;

[ApiController]
[Route("api/bookings")]
public class BookingsController : ControllerBase
{
    private readonly IBookingRepository _repository;
    private readonly IValidator<BookingRequestDto> _validator;

    public BookingsController(IBookingRepository repository, IValidator<BookingRequestDto> validator)
    {
        _repository = repository;
        _validator = validator;
    }

    [HttpPost]
    [EnableRateLimiting("booking")]
    public async Task<IActionResult> Create(
        [FromBody] BookingRequestDto dto, CancellationToken ct)
    {
        var validation = await _validator.ValidateAsync(dto, ct);
        if (!validation.IsValid)
            return ValidationProblem(new ValidationProblemDetails(validation.ToDictionary()));

        var reference = GenerateReference();

        var booking = new Booking
        {
            Reference = reference,
            FlightId = dto.FlightId,
            OriginCode = dto.OriginCode.ToUpperInvariant(),
            DestinationCode = dto.DestinationCode.ToUpperInvariant(),
            CabinClass = dto.CabinClass,
            TotalPrice = dto.TotalPrice,
            CreatedAt = DateTimeOffset.UtcNow,
            Passengers = dto.Passengers.Select(p => new Passenger
            {
                FullName = p.FullName.Trim(),
                Email = p.Email.Trim().ToLowerInvariant(),
                DocumentType = p.DocumentType,
                DocumentNumber = p.DocumentNumber.Trim().ToUpperInvariant()
            }).ToList()
        };

        await _repository.SaveAsync(booking, ct);

        return CreatedAtAction(nameof(GetByReference), new { reference },
            new BookingResponseDto(booking.Reference, booking.CreatedAt, booking.Passengers.Count));
    }

    [HttpGet("{reference}")]
    public async Task<IActionResult> GetByReference(string reference, CancellationToken ct)
    {
        var booking = await _repository.GetByReferenceAsync(reference, ct);
        return booking is null ? NotFound() : Ok(booking);
    }

    private static string GenerateReference()
    {
        const string chars = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";
        var span = Random.Shared.GetItems<char>(chars, 6);
        return $"SR-{new string(span)}";
    }
}
