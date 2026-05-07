using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SkyRoute.Domain.Interfaces;
using SkyRoute.Infrastructure.Persistence;
using SkyRoute.Infrastructure.Pricing;
using SkyRoute.Infrastructure.Providers;
using SkyRoute.Infrastructure.Services;

namespace SkyRoute.Infrastructure.Extensions;

public static class InfrastructureServiceExtensions
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<SkyRouteDbContext>(opts =>
            opts.UseSqlite(
                configuration.GetConnectionString("Default") ?? "Data Source=skyroute.db"));

        services.AddScoped<IBookingRepository, EfCoreBookingRepository>();

        services.AddSingleton<GlobalAirPricingStrategy>();
        services.AddSingleton<BudgetWingsPricingStrategy>();
        services.AddSingleton<ArcticAirPricingStrategy>();

        services.AddSingleton<IFlightProvider, GlobalAirProvider>();
        services.AddSingleton<IFlightProvider, BudgetWingsProvider>();
        services.AddSingleton<IFlightProvider, ArcticAirProvider>();

        services.AddSingleton<IFlightAggregator, FlightAggregator>();

        return services;
    }
}
