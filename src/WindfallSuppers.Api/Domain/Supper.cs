namespace WindfallSuppers.Api.Domain;

/// <summary>
/// A supper a host offers to neighbours. The booking rules live here, on the
/// entity, so every caller (API, tests, a future admin tool) gets the same rules.
/// </summary>
public class Supper
{
    public const int MaxSeats = 8;

    private readonly List<Booking> _bookings = [];

    // For EF Core.
    private Supper() { }

    public Supper(string hostId, string title, string area, DateTime startsAtUtc, int seats, DateTime nowUtc)
    {
        if (string.IsNullOrWhiteSpace(hostId)) throw new DomainException(DomainError.Invalid, "A supper needs a host.");
        if (string.IsNullOrWhiteSpace(title)) throw new DomainException(DomainError.Invalid, "A supper needs a title.");
        if (string.IsNullOrWhiteSpace(area)) throw new DomainException(DomainError.Invalid, "A supper needs an area.");
        if (seats is < 1 or > MaxSeats) throw new DomainException(DomainError.Invalid, $"Seats must be between 1 and {MaxSeats}.");
        if (startsAtUtc <= nowUtc) throw new DomainException(DomainError.Invalid, "A supper must start in the future.");

        Id = Guid.NewGuid();
        HostId = hostId;
        Title = title.Trim();
        Area = area.Trim();
        StartsAtUtc = startsAtUtc;
        Seats = seats;
    }

    public Guid Id { get; private set; }
    public string HostId { get; private set; } = "";
    public string Title { get; private set; } = "";
    public string Area { get; private set; } = "";
    public DateTime StartsAtUtc { get; private set; }
    public int Seats { get; private set; }

    /// <summary>
    /// Bumped on every booking change. EF Core uses it as a concurrency token,
    /// so two guests grabbing the last seat at once can't both succeed.
    /// </summary>
    public int Version { get; private set; }

    public IReadOnlyCollection<Booking> Bookings => _bookings;

    public int SeatsTaken => _bookings.Sum(b => b.Seats);
    public int SeatsLeft => Seats - SeatsTaken;

    public Booking Book(string guestId, int seats, DateTime nowUtc)
    {
        if (string.IsNullOrWhiteSpace(guestId)) throw new DomainException(DomainError.Invalid, "A booking needs a guest.");
        if (guestId == HostId) throw new DomainException(DomainError.Conflict, "Hosts can't book their own supper.");
        if (StartsAtUtc <= nowUtc) throw new DomainException(DomainError.Conflict, "This supper has already started.");
        if (seats < 1) throw new DomainException(DomainError.Invalid, "Book at least one seat.");
        if (_bookings.Any(b => b.GuestId == guestId)) throw new DomainException(DomainError.Conflict, "You've already booked this supper.");
        if (seats > SeatsLeft) throw new DomainException(DomainError.Conflict, $"Only {SeatsLeft} seat(s) left.");

        var booking = new Booking(Id, guestId, seats, nowUtc);
        _bookings.Add(booking);
        Version++;
        return booking;
    }

    public void CancelBooking(string guestId, DateTime nowUtc)
    {
        var booking = _bookings.SingleOrDefault(b => b.GuestId == guestId)
            ?? throw new DomainException(DomainError.NotFound, "You don't have a booking for this supper.");
        if (StartsAtUtc <= nowUtc) throw new DomainException(DomainError.Conflict, "This supper has already started.");

        _bookings.Remove(booking);
        Version++;
    }
}
