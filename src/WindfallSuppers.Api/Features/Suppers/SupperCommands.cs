using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using WindfallSuppers.Api.Data;
using WindfallSuppers.Api.Domain;

namespace WindfallSuppers.Api.Features.Suppers;

public record CreateSupper(string Title, string Area, DateTime StartsAtUtc, int Seats);
public record BookSeats(int Seats);
public record BookingCreated(Guid BookingId, Guid SupperId, int Seats);

/// <summary>
/// Write side. Each command loads the aggregate, lets the domain apply its
/// rules, then saves. Handlers hold no business rules themselves.
/// </summary>
public class SupperCommands(SuppersDbContext db, TimeProvider clock)
{
    private DateTime Now => clock.GetUtcNow().UtcDateTime;

    public async Task<Guid> Create(string hostId, CreateSupper command, CancellationToken ct)
    {
        var supper = new Supper(hostId, command.Title, command.Area, command.StartsAtUtc.ToUniversalTime(), command.Seats, Now);
        db.Suppers.Add(supper);
        await db.SaveChangesAsync(ct);
        return supper.Id;
    }

    public async Task<BookingCreated> Book(Guid supperId, string guestId, BookSeats command, CancellationToken ct)
    {
        var supper = await LoadWithBookings(supperId, ct);
        var booking = supper.Book(guestId, command.Seats, Now);
        await SaveOrConflict(ct);
        return new BookingCreated(booking.Id, supper.Id, booking.Seats);
    }

    public async Task Cancel(Guid supperId, string guestId, CancellationToken ct)
    {
        var supper = await LoadWithBookings(supperId, ct);
        supper.CancelBooking(guestId, Now);
        await SaveOrConflict(ct);
    }

    private async Task<Supper> LoadWithBookings(Guid id, CancellationToken ct) =>
        await db.Suppers.Include(s => s.Bookings).SingleOrDefaultAsync(s => s.Id == id, ct)
        ?? throw new DomainException(DomainError.NotFound, "Supper not found.");

    private async Task SaveOrConflict(CancellationToken ct)
    {
        try
        {
            await db.SaveChangesAsync(ct);
        }
        catch (DbUpdateConcurrencyException)
        {
            // Someone else changed this supper's bookings between our read and our write.
            throw new DomainException(DomainError.Conflict, "Bookings changed while you were booking. Please try again.");
        }
        catch (DbUpdateException ex) when (ex.InnerException is SqliteException { SqliteErrorCode: 19 })
        {
            // SQLITE_CONSTRAINT: the unique (SupperId, GuestId) index caught a double booking.
            // Any other database error is left to surface as a 500 and be logged.
            throw new DomainException(DomainError.Conflict, "You've already booked this supper.");
        }
    }
}
