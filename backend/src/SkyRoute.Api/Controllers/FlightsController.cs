using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using SkyRoute.Api.DTOs;
using SkyRoute.Domain.Interfaces;
using SkyRoute.Domain.Models;

namespace SkyRoute.Api.Controllers;

[ApiController]
[Route("api/flights")]
public class FlightsController : ControllerBase
{
    private readonly IFlightAggregator _aggregator;
    private readonly IValidator<SearchRequestDto> _validator;

    public FlightsController(IFlightAggregator aggregator, IValidator<SearchRequestDto> validator)
    {
        _aggregator = aggregator;
        _validator = validator;
    }

    [HttpGet("search")]
    [EnableRateLimiting("search")]
    public async Task<IActionResult> Search(
        [FromQuery] string originCode,
        [FromQuery] string destinationCode,
        [FromQuery] DateOnly departureDate,
        [FromQuery] int passengers,
        [FromQuery] string cabinClass,
        CancellationToken ct)
    {
        var dto = new SearchRequestDto(
            originCode ?? string.Empty,
            destinationCode ?? string.Empty,
            departureDate,
            passengers,
            cabinClass ?? string.Empty);

        var validation = await _validator.ValidateAsync(dto, ct);
        if (!validation.IsValid)
            return ValidationProblem(new ValidationProblemDetails(validation.ToDictionary()));

        var request = new SearchRequest(
            dto.OriginCode.ToUpperInvariant(),
            dto.DestinationCode.ToUpperInvariant(),
            dto.DepartureDate,
            dto.Passengers,
            dto.CabinClass);

        var offers = await _aggregator.SearchAsync(request, ct);

        var results = offers.Select(o => new FlightOfferDto(
            o.Id, o.Provider, o.FlightNumber,
            o.OriginCode, o.DestinationCode,
            o.DepartureTime, o.ArrivalTime, o.DurationMinutes,
            o.CabinClass, o.PricePerPassenger, o.TotalPrice,
            o.Passengers, o.Currency)).ToList();

        return Ok(new SearchResponseDto(results));
    }
}
