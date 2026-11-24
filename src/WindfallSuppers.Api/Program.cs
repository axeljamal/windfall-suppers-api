using Microsoft.EntityFrameworkCore;
using Scalar.AspNetCore;
using WindfallSuppers.Api;
using WindfallSuppers.Api.Data;
using WindfallSuppers.Api.Features.Suppers;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<SuppersDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("Suppers") ?? "Data Source=suppers.db"));

builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddScoped<SupperQueries>();
builder.Services.AddScoped<SupperCommands>();

builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<ProblemDetailsExceptionHandler>();
builder.Services.AddOpenApi();

var app = builder.Build();

app.UseExceptionHandler();

// Fine for a single-instance demo on SQLite. With several instances or SQL Server,
// run migrations as a separate deploy step instead (see docs/DECISIONS.md).
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<SuppersDbContext>();
    db.Database.Migrate();
    if (app.Environment.IsDevelopment()) DemoData.SeedIfEmpty(db, TimeProvider.System);
}

app.MapOpenApi();
if (app.Environment.IsDevelopment())
{
    // Interactive API docs at /scalar for trying the endpoints in a browser.
    app.MapScalarApiReference(o => o.WithTitle("Windfall Suppers API"));
    app.MapGet("/", () => Results.Redirect("/scalar")).ExcludeFromDescription();
}
app.MapGet("/health", () => TypedResults.Ok(new { status = "ok" }));
app.MapSupperEndpoints();

app.Run();

// Lets the integration tests start the app with WebApplicationFactory<Program>.
public partial class Program;
