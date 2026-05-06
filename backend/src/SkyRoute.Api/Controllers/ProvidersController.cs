using Microsoft.AspNetCore.Mvc;
using SkyRoute.Domain.Interfaces;

namespace SkyRoute.Api.Controllers;

/// <summary>
/// Lists the airline providers currently registered in the platform.
/// Adding a new provider is a two-line change (one class implementing
/// <see cref="IFlightProvider"/> + a DI registration) — this endpoint
/// proves the seam works at runtime without code changes here.
/// </summary>
[ApiController]
[Route("api/providers")]
public class ProvidersController : ControllerBase
{
    private readonly IEnumerable<IFlightProvider> _providers;

    public ProvidersController(IEnumerable<IFlightProvider> providers) => _providers = providers;

    [HttpGet]
    public IActionResult GetAll() =>
        Ok(_providers
            .Select(p => new { providerId = p.ProviderId })
            .OrderBy(p => p.providerId));
}
