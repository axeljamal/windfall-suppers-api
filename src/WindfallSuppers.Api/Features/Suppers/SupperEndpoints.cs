using Microsoft.AspNetCore.Mvc;

namespace WindfallSuppers.Api.Features.Suppers;

public static class SupperEndpoints
{
    // Stand-in for real authentication: the caller's user id arrives in a header.
    // In production this would come from a validated token (e.g. JWT claims).
    public const string UserHeader = "X-User-Id";

    public static IEndpointRouteBuilder MapSupperEndpoints(this IEndpointRouteBuilder app)
    {
        var suppers = app.MapGroup("/suppers").WithTags("Suppers");

        suppers.MapGet("/", async (string? area, SupperQueries queries, CancellationToken ct) =>
            TypedResults.Ok(await queries.ListUpcoming(area, ct)));

        suppers.MapGet("/{id:guid}", async (Guid id, SupperQueries queries, CancellationToken ct) =>
            TypedResults.Ok(await queries.Get(id, ct)))
            .WithName("GetSupper");

        suppers.MapPost("/", async (CreateSupper command, [FromHeader(Name = UserHeader)] string? userId, SupperCommands commands, CancellationToken ct) =>
        {
            var id = await commands.Create(Require(userId), command, ct);
            return TypedResults.CreatedAtRoute(new { id }, "GetSupper", new { id });
        });

        suppers.MapPost("/{id:guid}/bookings", async (Guid id, BookSeats command, [FromHeader(Name = UserHeader)] string? userId, SupperCommands commands, CancellationToken ct) =>
        {
            var booking = await commands.Book(id, Require(userId), command, ct);
            return TypedResults.Created($"/suppers/{id}/bookings/mine", booking);
        });

        suppers.MapDelete("/{id:guid}/bookings/mine", async (Guid id, [FromHeader(Name = UserHeader)] string? userId, SupperCommands commands, CancellationToken ct) =>
        {
            await commands.Cancel(id, Require(userId), ct);
            return TypedResults.NoContent();
        });

        return app;
    }

    // Declared as a header parameter so it shows up in the OpenAPI docs.
    private static string Require(string? id)
    {
        return string.IsNullOrWhiteSpace(id)
            ? throw new UnauthorizedAccessException($"Send your user id in the {UserHeader} header.")
            : id;
    }
}
