using Microsoft.EntityFrameworkCore;
using SkyRoute.Domain.Entities;
using SkyRoute.Domain.Interfaces;

namespace SkyRoute.Infrastructure.Persistence;

public class EfCoreBookingRepository : IBookingRepository
{
    private readonly SkyRouteDbContext _db;

    public EfCoreBookingRepository(SkyRouteDbContext db) => _db = db;

    public async Task<Booking> SaveAsync(Booking booking, CancellationToken ct = default)
    {
        _db.Bookings.Add(booking);
        await _db.SaveChangesAsync(ct);
        return booking;
    }

    public Task<Booking?> GetByReferenceAsync(string reference, CancellationToken ct = default) =>
        _db.Bookings
           .Include(b => b.Passengers)
           .FirstOrDefaultAsync(b => b.Reference == reference, ct);
}
