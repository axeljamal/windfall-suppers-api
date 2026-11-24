using WindfallSuppers.Api.Domain;

namespace WindfallSuppers.Api.Data;

/// <summary>Sample Brighton and Hove suppers so a fresh local run has something to show.</summary>
public static class DemoData
{
    public static void SeedIfEmpty(SuppersDbContext db, TimeProvider clock)
    {
        if (db.Suppers.Any()) return;

        var now = clock.GetUtcNow().UtcDateTime;
        var evening = now.Date.AddDays(2).AddHours(18).AddMinutes(30);

        var soup = new Supper("host-maya", "Squash soup and sourdough", "Hove", evening, 6, now);
        var pie = new Supper("host-tom", "Leek and potato pie", "Kemptown", evening.AddDays(1), 4, now);
        var curry = new Supper("host-sri", "Veg rendang with rice", "Portslade", evening.AddDays(3), 8, now);

        soup.Book("guest-ana", 2, now);
        pie.Book("guest-joe", 3, now);

        db.Suppers.AddRange(soup, pie, curry);
        db.SaveChanges();
    }
}
