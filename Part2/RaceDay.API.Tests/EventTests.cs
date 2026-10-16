using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace RaceDay.API.Tests;

public class EventTests
{
    private static HttpClient CreateClient(RaceDayApiFactory factory)
    {
        return factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("https://localhost"),
            HandleCookies = true,
            AllowAutoRedirect = false
        });
    }

    private static async Task SignIn(
        HttpClient client, string email, string role)
    {
        var registration = await client.PostAsJsonAsync(
            "/api/auth/register", new
            {
                firstName = "Event",
                lastName = "Tester",
                email,
                password = "RaceDayDemo!2026",
                role
            });

        Assert.Equal(HttpStatusCode.Created, registration.StatusCode);

        var login = await client.PostAsJsonAsync("/api/auth/login", new
        {
            email,
            password = "RaceDayDemo!2026"
        });

        Assert.Equal(HttpStatusCode.OK, login.StatusCode);
    }

    private static object EventBody(decimal distance = 5)
    {
        return new
        {
            eventName = "Test Community Run",
            description = "Automated test event.",
            eventDate = DateTime.Today.AddDays(30).ToString("yyyy-MM-dd"),
            location = "Pretoria",
            distance,
            eventTypeID = 1
        };
    }

    [Fact]
    public async Task Organiser_CanCreateAndRetrieveEvent()
    {
        using var factory = new RaceDayApiFactory();
        using var client = CreateClient(factory);
        await SignIn(client, "organiser@example.com", "Organiser");

        var created = await client.PostAsJsonAsync(
            "/api/events", EventBody());

        Assert.Equal(HttpStatusCode.Created, created.StatusCode);

        var body = await created.Content.ReadFromJsonAsync<JsonElement>();
        int eventId = body.GetProperty("eventID").GetInt32();

        var retrieved = await client.GetAsync($"/api/events/{eventId}");
        Assert.Equal(HttpStatusCode.OK, retrieved.StatusCode);

        var result = await retrieved.Content
            .ReadFromJsonAsync<JsonElement>();

        Assert.Equal(
            "Test Community Run",
            result.GetProperty("eventName").GetString());

        Assert.Equal(5m, result.GetProperty("distance").GetDecimal());
    }

    [Fact]
    public async Task Participant_CannotCreateEvent()
    {
        using var factory = new RaceDayApiFactory();
        using var client = CreateClient(factory);
        await SignIn(client, "participant@example.com", "Participant");

        var response = await client.PostAsJsonAsync(
            "/api/events", EventBody());

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);

        var events = await client.GetFromJsonAsync<JsonElement>(
            "/api/events");

        Assert.Equal(0, events.GetArrayLength());
    }

    [Fact]
    public async Task AnonymousUser_CannotCreateEvent()
    {
        using var factory = new RaceDayApiFactory();
        using var client = CreateClient(factory);

        var response = await client.PostAsJsonAsync(
            "/api/events", EventBody());

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-5)]
    [InlineData(10000)]
    public async Task InvalidDistance_ReturnsBadRequest(int distance)
    {
        using var factory = new RaceDayApiFactory();
        using var client = CreateClient(factory);
        await SignIn(client, "organiser@example.com", "Organiser");

        var response = await client.PostAsJsonAsync(
            "/api/events", EventBody(distance));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task MissingEvent_ReturnsNotFound()
    {
        using var factory = new RaceDayApiFactory();
        using var client = CreateClient(factory);

        var response = await client.GetAsync("/api/events/99999");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task DifferentOrganiser_CannotUpdateAnotherOwnersEvent()
    {
        using var factory = new RaceDayApiFactory();
        using var owner = CreateClient(factory);
        using var other = CreateClient(factory);

        await SignIn(owner, "owner@example.com", "Organiser");
        await SignIn(other, "other@example.com", "Organiser");

        var created = await owner.PostAsJsonAsync(
            "/api/events", EventBody());

        Assert.Equal(HttpStatusCode.Created, created.StatusCode);

        var body = await created.Content.ReadFromJsonAsync<JsonElement>();
        int eventId = body.GetProperty("eventID").GetInt32();

        var response = await other.PutAsJsonAsync(
            $"/api/events/{eventId}", EventBody());

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }
}