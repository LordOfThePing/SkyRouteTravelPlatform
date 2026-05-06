using SkyRoute.Domain.Entities;

namespace SkyRoute.Domain.Interfaces;

public interface IBookingRepository
{
    Task<Booking> SaveAsync(Booking booking, CancellationToken ct = default);
    Task<Booking?> GetByReferenceAsync(string reference, CancellationToken ct = default);
}
