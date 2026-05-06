using Microsoft.EntityFrameworkCore;
using SkyRoute.Domain.Entities;
using SkyRoute.Infrastructure.Persistence;

namespace SkyRoute.Tests.Persistence;

public class BookingRepositoryTests
{
    [Fact]
    public async Task SaveAsync_PersistsBookingWithPassengerChildren()
    {
        await using var db = CreateDbContext();
        var repository = new EfCoreBookingRepository(db);
        var booking = BuildBooking("SR-AAAA11");

        await repository.SaveAsync(booking);

        var stored = await db.Bookings.Include(x => x.Passengers).SingleAsync();
        stored.Reference.Should().Be("SR-AAAA11");
        stored.Passengers.Should().HaveCount(2);
        stored.Passengers.Select(p => p.FullName).Should().Contain(["Ada Lovelace", "Alan Turing"]);
    }

    [Fact]
    public async Task GetByReferenceAsync_ReturnsPersistedBooking()
    {
        await using var db = CreateDbContext();
        var repository = new EfCoreBookingRepository(db);
        var booking = BuildBooking("SR-BBBB22");
        await repository.SaveAsync(booking);

        var result = await repository.GetByReferenceAsync("SR-BBBB22");

        result.Should().NotBeNull();
        result!.Reference.Should().Be("SR-BBBB22");
        result.Passengers.Should().HaveCount(2);
    }

    private static SkyRouteDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<SkyRouteDbContext>()
            .UseInMemoryDatabase(databaseName: $"skyroute-tests-{Guid.NewGuid()}")
            .Options;

        return new SkyRouteDbContext(options);
    }

    private static Booking BuildBooking(string reference) =>
        new()
        {
            Reference = reference,
            FlightId = "GA-1234",
            OriginCode = "MAD",
            DestinationCode = "JFK",
            CabinClass = "Business",
            TotalPrice = 1200m,
            Currency = "USD",
            CreatedAt = DateTimeOffset.UtcNow,
            Passengers =
            [
                new Passenger
                {
                    FullName = "Ada Lovelace",
                    Email = "ada@example.com",
                    DocumentType = "Passport",
                    DocumentNumber = "X1234567"
                },
                new Passenger
                {
                    FullName = "Alan Turing",
                    Email = "alan@example.com",
                    DocumentType = "Passport",
                    DocumentNumber = "Y7654321"
                }
            ]
        };
}
