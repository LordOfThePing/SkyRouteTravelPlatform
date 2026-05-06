using SkyRoute.Domain.Models;

namespace SkyRoute.Infrastructure.Data;

public static class AirportCatalog
{
    private static readonly Dictionary<string, Airport> _airports = new(StringComparer.OrdinalIgnoreCase)
    {
        ["MAD"] = new("MAD", "Adolfo Suárez Madrid-Barajas", "Madrid", "Spain"),
        ["BCN"] = new("BCN", "Josep Tarradellas Barcelona-El Prat", "Barcelona", "Spain"),
        ["AGP"] = new("AGP", "Málaga Costa del Sol", "Málaga", "Spain"),
        ["JFK"] = new("JFK", "John F. Kennedy International", "New York", "United States"),
        ["LAX"] = new("LAX", "Los Angeles International", "Los Angeles", "United States"),
        ["MIA"] = new("MIA", "Miami International", "Miami", "United States"),
        ["LHR"] = new("LHR", "Heathrow", "London", "United Kingdom"),
        ["CDG"] = new("CDG", "Charles de Gaulle", "Paris", "France"),
    };

    public static IReadOnlyDictionary<string, Airport> All => _airports;

    public static Airport? Get(string code) =>
        _airports.TryGetValue(code, out var airport) ? airport : null;

    public static bool Exists(string code) => _airports.ContainsKey(code);

    public static bool IsSameCountry(string originCode, string destinationCode)
    {
        var origin = Get(originCode);
        var destination = Get(destinationCode);
        return origin is not null && destination is not null &&
               string.Equals(origin.Country, destination.Country, StringComparison.OrdinalIgnoreCase);
    }
}
