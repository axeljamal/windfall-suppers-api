namespace WindfallSuppers.Api.Domain;

public class Booking
{
    // For EF Core.
    private Booking() { }

    internal Booking(Guid supperId, string guestId, int seats, DateTime createdAtUtc)
    {
        Id = Guid.NewGuid();
        SupperId = supperId;
        GuestId = guestId;
        Seats = seats;
        CreatedAtUtc = createdAtUtc;
    }

    public Guid Id { get; private set; }
    public Guid SupperId { get; private set; }
    public string GuestId { get; private set; } = "";
    public int Seats { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }
}
