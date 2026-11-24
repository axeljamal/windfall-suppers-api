using Microsoft.EntityFrameworkCore;
using WindfallSuppers.Api.Data;
using WindfallSuppers.Api.Domain;

namespace WindfallSuppers.Api.Features.Suppers;

public record SupperSummary(Guid Id, string Title, string Area, DateTime StartsAtUtc, int Seats, int SeatsLeft);

/// <summary>
/// Read side. Queries never change state, use AsNoTracking and project straight
/// to the shape the client needs, so they stay fast as the tables grow.
/// </summary>
public class SupperQueries(SuppersDbContext db, TimeProvider clock)
{
    public async Task<IReadOnlyList<SupperSummary>> ListUpcoming(string? area, CancellationToken ct)
    {
        var now = clock.GetUtcNow().UtcDateTime;
        var query = db.Suppers.AsNoTracking().Where(s => s.StartsAtUtc > now);

        if (!string.IsNullOrWhiteSpace(area))
        {
            var wanted = area.Trim().ToLower();
            query = query.Where(s => s.Area.ToLower() == wanted);
        }

        return await query
            .OrderBy(s => s.StartsAtUtc)
            .Select(s => new SupperSummary(
                s.Id, s.Title, s.Area, s.StartsAtUtc, s.Seats,
                s.Seats - s.Bookings.Sum(b => b.Seats)))
            .ToListAsync(ct);
    }

    public async Task<SupperSummary> Get(Guid id, CancellationToken ct)
    {
        return await db.Suppers.AsNoTracking()
            .Where(s => s.Id == id)
            .Select(s => new SupperSummary(
                s.Id, s.Title, s.Area, s.StartsAtUtc, s.Seats,
                s.Seats - s.Bookings.Sum(b => b.Seats)))
            .SingleOrDefaultAsync(ct)
            ?? throw new DomainException(DomainError.NotFound, "Supper not found.");
    }
}
