using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc;
using WindfallSuppers.Api.Features.Suppers;

namespace WindfallSuppers.Tests;

public class SupperApiTests(ApiFactory factory) : IClassFixture<ApiFactory>
{
    private HttpClient ClientFor(string userId)
    {
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add(SupperEndpoints.UserHeader, userId);
        return client;
    }

    private async Task<Guid> CreateSupper(string area = "Kemptown", int seats = 4)
    {
        var response = await ClientFor("host-1").PostAsJsonAsync("/suppers",
            new CreateSupper("Leek and potato pie", area, ApiFactory.Start.UtcDateTime.AddDays(3), seats));
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<IdResponse>();
        return body!.Id;
    }

    private record IdResponse(Guid Id);

    [Fact]
    public async Task Health_check_is_ok()
    {
        var response = await factory.CreateClient().GetAsync("/health");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Created_supper_can_be_read_back()
    {
        var id = await CreateSupper();

        var supper = await factory.CreateClient().GetFromJsonAsync<SupperSummary>($"/suppers/{id}");

        Assert.Equal("Leek and potato pie", supper!.Title);
        Assert.Equal(4, supper.SeatsLeft);
    }

    [Fact]
    public async Task Dates_come_back_as_utc()
    {
        var id = await CreateSupper();

        var json = await factory.CreateClient().GetStringAsync($"/suppers/{id}");

        Assert.Contains("\"startsAtUtc\":\"2026-12-04T12:00:00Z\"", json);
    }

    [Fact]
    public async Task List_filters_by_area_case_insensitively()
    {
        var id = await CreateSupper(area: "Portslade");

        var suppers = await factory.CreateClient().GetFromJsonAsync<List<SupperSummary>>("/suppers?area=portslade");

        Assert.Contains(suppers!, s => s.Id == id);
        Assert.All(suppers!, s => Assert.Equal("Portslade", s.Area));
    }

    [Fact]
    public async Task Booking_reduces_seats_left()
    {
        var id = await CreateSupper(seats: 4);

        var response = await ClientFor("guest-1").PostAsJsonAsync($"/suppers/{id}/bookings", new BookSeats(3));

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var supper = await factory.CreateClient().GetFromJsonAsync<SupperSummary>($"/suppers/{id}");
        Assert.Equal(1, supper!.SeatsLeft);
    }

    [Fact]
    public async Task Overbooking_returns_409_with_problem_details()
    {
        var id = await CreateSupper(seats: 2);
        await ClientFor("guest-1").PostAsJsonAsync($"/suppers/{id}/bookings", new BookSeats(2));

        var response = await ClientFor("guest-2").PostAsJsonAsync($"/suppers/{id}/bookings", new BookSeats(1));

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>();
        Assert.Equal("Only 0 seat(s) left.", problem!.Detail);
    }

    [Fact]
    public async Task Cancelling_a_booking_frees_the_seats()
    {
        var id = await CreateSupper(seats: 2);
        var guest = ClientFor("guest-1");
        await guest.PostAsJsonAsync($"/suppers/{id}/bookings", new BookSeats(2));

        var response = await guest.DeleteAsync($"/suppers/{id}/bookings/mine");

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        var supper = await factory.CreateClient().GetFromJsonAsync<SupperSummary>($"/suppers/{id}");
        Assert.Equal(2, supper!.SeatsLeft);
    }

    [Fact]
    public async Task Unknown_supper_returns_404()
    {
        var response = await factory.CreateClient().GetAsync($"/suppers/{Guid.NewGuid()}");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Writing_without_a_user_returns_401()
    {
        var response = await factory.CreateClient().PostAsJsonAsync("/suppers",
            new CreateSupper("No host", "Hove", ApiFactory.Start.UtcDateTime.AddDays(1), 2));
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Invalid_seat_count_returns_400()
    {
        var response = await ClientFor("host-1").PostAsJsonAsync("/suppers",
            new CreateSupper("Too big", "Hove", ApiFactory.Start.UtcDateTime.AddDays(1), 20));
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}
