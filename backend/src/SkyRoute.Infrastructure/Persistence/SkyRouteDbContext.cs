using Microsoft.EntityFrameworkCore;
using SkyRoute.Domain.Entities;

namespace SkyRoute.Infrastructure.Persistence;

public class SkyRouteDbContext : DbContext
{
    public SkyRouteDbContext(DbContextOptions<SkyRouteDbContext> options) : base(options) { }

    public DbSet<Booking> Bookings => Set<Booking>();
    public DbSet<Passenger> Passengers => Set<Passenger>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Booking>(b =>
        {
            b.HasKey(x => x.Id);
            b.Property(x => x.Reference).HasMaxLength(20).IsRequired();
            b.Property(x => x.FlightId).HasMaxLength(50).IsRequired();
            b.Property(x => x.OriginCode).HasMaxLength(3).IsRequired();
            b.Property(x => x.DestinationCode).HasMaxLength(3).IsRequired();
            b.Property(x => x.CabinClass).HasMaxLength(20).IsRequired();
            b.Property(x => x.TotalPrice).HasColumnType("decimal(10,2)");
            b.Property(x => x.Currency).HasMaxLength(3).HasDefaultValue("USD");
            b.HasIndex(x => x.Reference).IsUnique();
            b.HasMany(x => x.Passengers)
             .WithOne(x => x.Booking)
             .HasForeignKey(x => x.BookingId)
             .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Passenger>(p =>
        {
            p.HasKey(x => x.Id);
            p.Property(x => x.FullName).HasMaxLength(100).IsRequired();
            p.Property(x => x.Email).HasMaxLength(254).IsRequired();
            p.Property(x => x.DocumentType).HasMaxLength(20).IsRequired();
            p.Property(x => x.DocumentNumber).HasMaxLength(32).IsRequired();
        });
    }
}
