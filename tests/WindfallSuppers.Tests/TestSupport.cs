using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using WindfallSuppers.Api.Data;

namespace WindfallSuppers.Tests;

/// <summary>A clock the tests control, so "has this supper started?" is deterministic.</summary>
public class FixedClock(DateTimeOffset now) : TimeProvider
{
    public DateTimeOffset Now { get; set; } = now;
    public override DateTimeOffset GetUtcNow() => Now;
}

/// <summary>
/// Starts the real app in memory, swapping the file database for an in-memory
/// SQLite one per test class. Migrations still run, so the tests cover the schema too.
/// </summary>
public class ApiFactory : WebApplicationFactory<Program>
{
    public static readonly DateTimeOffset Start = new(2026, 12, 1, 12, 0, 0, TimeSpan.Zero);
    public FixedClock Clock { get; } = new(Start);

    private readonly SqliteConnection _connection = new("Data Source=:memory:");

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing"); // no demo data or docs UI in tests
        _connection.Open();
        builder.ConfigureServices(services =>
        {
            services.RemoveAll<DbContextOptions<SuppersDbContext>>();
            services.RemoveAll<IDbContextOptionsConfiguration<SuppersDbContext>>();
            services.AddDbContext<SuppersDbContext>(o => o.UseSqlite(_connection));

            services.RemoveAll<TimeProvider>();
            services.AddSingleton<TimeProvider>(Clock);
        });
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        _connection.Dispose();
    }
}
