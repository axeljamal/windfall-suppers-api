using WindfallSuppers.Api.Domain;

namespace WindfallSuppers.Tests;

public class SupperRulesTests
{
    private static readonly DateTime Now = new(2026, 12, 1, 12, 0, 0, DateTimeKind.Utc);

    private static Supper NewSupper(int seats = 4) =>
        new("host-1", "Squash soup and bread", "Hove", Now.AddDays(2), seats, Now);

    [Fact]
    public void Booking_takes_seats()
    {
        var supper = NewSupper();
        supper.Book("guest-1", 2, Now);
        Assert.Equal(2, supper.SeatsLeft);
    }

    [Fact]
    public void Host_cannot_book_own_supper()
    {
        var ex = Assert.Throws<DomainException>(() => NewSupper().Book("host-1", 1, Now));
        Assert.Equal(DomainError.Conflict, ex.Error);
    }

    [Fact]
    public void Cannot_book_more_seats_than_are_left()
    {
        var supper = NewSupper(seats: 3);
        supper.Book("guest-1", 2, Now);
        var ex = Assert.Throws<DomainException>(() => supper.Book("guest-2", 2, Now));
        Assert.Equal("Only 1 seat(s) left.", ex.Message);
    }

    [Fact]
    public void Guest_can_only_book_once()
    {
        var supper = NewSupper();
        supper.Book("guest-1", 1, Now);
        Assert.Throws<DomainException>(() => supper.Book("guest-1", 1, Now));
    }

    [Fact]
    public void Cannot_book_after_the_supper_starts()
    {
        var supper = NewSupper();
        Assert.Throws<DomainException>(() => supper.Book("guest-1", 1, Now.AddDays(3)));
    }

    [Fact]
    public void Cancelling_frees_the_seats_and_bumps_the_version()
    {
        var supper = NewSupper();
        supper.Book("guest-1", 3, Now);
        var versionAfterBooking = supper.Version;

        supper.CancelBooking("guest-1", Now);

        Assert.Equal(4, supper.SeatsLeft);
        Assert.True(supper.Version > versionAfterBooking);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(Supper.MaxSeats + 1)]
    public void Seats_must_be_between_one_and_eight(int seats)
    {
        var ex = Assert.Throws<DomainException>(() => NewSupper(seats));
        Assert.Equal(DomainError.Invalid, ex.Error);
    }

    [Fact]
    public void Supper_must_start_in_the_future()
    {
        Assert.Throws<DomainException>(() => new Supper("host-1", "Late lunch", "Hove", Now.AddMinutes(-1), 4, Now));
    }
}
