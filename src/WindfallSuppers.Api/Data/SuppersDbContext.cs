using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using WindfallSuppers.Api.Domain;

namespace WindfallSuppers.Api.Data;

public class SuppersDbContext(DbContextOptions<SuppersDbContext> options) : DbContext(options)
{
    public DbSet<Supper> Suppers => Set<Supper>();
    public DbSet<Booking> Bookings => Set<Booking>();

    protected override void ConfigureConventions(ModelConfigurationBuilder conventions)
    {
        // SQLite stores dates as text and forgets they are UTC. Mark them UTC on the way
        // back out so the API always returns "...Z" timestamps.
        conventions.Properties<DateTime>().HaveConversion<UtcDateTimeConverter>();
    }

    private class UtcDateTimeConverter() : ValueConverter<DateTime, DateTime>(
        v => v.ToUniversalTime(),
        v => DateTime.SpecifyKind(v, DateTimeKind.Utc));

    protected override void OnModelCreating(ModelBuilder model)
    {
        model.Entity<Supper>(supper =>
        {
            // Ids are made in C# (Guid.NewGuid), not by the database. Without this, EF treats a
            // new booking with an id as an existing row and sends UPDATE instead of INSERT.
            supper.HasKey(s => s.Id);
            supper.Property(s => s.Id).ValueGeneratedNever();
            supper.Property(s => s.HostId).HasMaxLength(64).IsRequired();
            supper.Property(s => s.Title).HasMaxLength(120).IsRequired();
            supper.Property(s => s.Area).HasMaxLength(60).IsRequired();
            supper.Property(s => s.Version).IsConcurrencyToken();
            supper.HasIndex(s => new { s.Area, s.StartsAtUtc });

            // Bookings are only changed through Supper, so EF writes to the private list.
            supper.HasMany(s => s.Bookings).WithOne().HasForeignKey(b => b.SupperId).OnDelete(DeleteBehavior.Cascade);
            supper.Navigation(s => s.Bookings).UsePropertyAccessMode(PropertyAccessMode.Field);

            supper.Ignore(s => s.SeatsTaken);
            supper.Ignore(s => s.SeatsLeft);
        });

        model.Entity<Booking>(booking =>
        {
            booking.HasKey(b => b.Id);
            booking.Property(b => b.Id).ValueGeneratedNever();
            booking.Property(b => b.GuestId).HasMaxLength(64).IsRequired();
            // Backs up the "one booking per guest" rule at the database level.
            booking.HasIndex(b => new { b.SupperId, b.GuestId }).IsUnique();
        });
    }
}
