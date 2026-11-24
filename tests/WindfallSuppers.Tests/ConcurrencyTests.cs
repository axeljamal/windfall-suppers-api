using Microsoft.Extensions.DependencyInjection;
using WindfallSuppers.Api.Features.Suppers;

namespace WindfallSuppers.Tests;

public class ConcurrencyTests(ApiFactory factory) : IClassFixture<ApiFactory>
{
    [Fact]
    public async Task Two_guests_racing_for_the_last_seat_cannot_both_win()
    {
        _ = factory.CreateClient(); // boots the app and runs migrations
        Guid supperId;
        using (var setup = factory.Services.CreateScope())
        {
            var commands = setup.ServiceProvider.GetRequiredService<SupperCommands>();
            supperId = await commands.Create("host-1",
                new CreateSupper("Last seat stew", "Hove", ApiFactory.Start.UtcDateTime.AddDays(1), 1), default);
        }

        // Two requests, each with its own DbContext, both read "1 seat left"...
        using var scopeA = factory.Services.CreateScope();
        using var scopeB = factory.Services.CreateScope();
        var dbA = scopeA.ServiceProvider.GetRequiredService<WindfallSuppers.Api.Data.SuppersDbContext>();
        var dbB = scopeB.ServiceProvider.GetRequiredService<WindfallSuppers.Api.Data.SuppersDbContext>();
        var supperA = await dbA.Suppers.FindAsync(supperId);
        var supperB = await dbB.Suppers.FindAsync(supperId);
        await dbA.Entry(supperA!).Collection(s => s.Bookings).LoadAsync();
        await dbB.Entry(supperB!).Collection(s => s.Bookings).LoadAsync();

        // ...both pass the in-memory rule check...
        supperA!.Book("guest-a", 1, ApiFactory.Start.UtcDateTime);
        supperB!.Book("guest-b", 1, ApiFactory.Start.UtcDateTime);

        // ...but only the first save wins; the Version token stops the second.
        await dbA.SaveChangesAsync();
        await Assert.ThrowsAsync<Microsoft.EntityFrameworkCore.DbUpdateConcurrencyException>(() => dbB.SaveChangesAsync());

        using var check = factory.Services.CreateScope();
        var summary = await check.ServiceProvider.GetRequiredService<SupperQueries>().Get(supperId, default);
        Assert.Equal(0, summary.SeatsLeft);
    }
}
