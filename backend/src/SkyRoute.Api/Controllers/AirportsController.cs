using Microsoft.AspNetCore.Mvc;
using SkyRoute.Api.DTOs;
using SkyRoute.Infrastructure.Data;

namespace SkyRoute.Api.Controllers;

[ApiController]
[Route("api/airports")]
public class AirportsController : ControllerBase
{
    [HttpGet]
    public IActionResult GetAll() =>
        Ok(AirportCatalog.All.Values
            .Select(a => new AirportDto(a.Code, a.Name, a.City, a.Country))
            .OrderBy(a => a.Country)
            .ThenBy(a => a.City));
}
