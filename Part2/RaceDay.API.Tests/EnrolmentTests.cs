using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using RaceDay.API.Data;
using RaceDay.API.Models;
using Xunit;

namespace RaceDay.API.Tests;

public class EnrolmentTests
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

    private static async Task<int> SignIn(
        HttpClient client, string email, string role)
    {
        var registration = await client.PostAsJsonAsync(
            "/api/auth/register", new
            {
                firstName = "Enrolment",
                lastName = "Tester",
                email,
                password = "RaceDayDemo!2026",
                role
            });

        Assert.Equal(HttpStatusCode.Created, registration.StatusCode);

        var account = await registration.Content
            .ReadFromJsonAsync<JsonElement>();

        var login = await client.PostAsJsonAsync("/api/auth/login", new
        {
            email,
            password = "RaceDayDemo!2026"
        });

        Assert.Equal(HttpStatusCode.OK, login.StatusCode);

        return account.GetProperty("userID").GetInt32();
    }

    private static async Task<int> SeedEnrolment(
        RaceDayApiFactory factory, int organiserId, int participantId)
    {
        using var scope = factory.Services.CreateScope();
        var database = scope.ServiceProvider
            .GetRequiredService<RaceDayDbContext>();

        var raceEvent = new RaceDay.API.Models.Event
        {
            OrganiserID = organiserId,
            EventTypeID = 1,
            EventName = "Enrolment Test Run",
            EventDate = DateTime.Today.AddDays(30),
            Location = "Pretoria",
            Distance = 5
        };

        database.Events.Add(raceEvent);
        await database.SaveChangesAsync();

        var category = new Category
        {
            EventID = raceEvent.EventID,
            CategoryName = "Open",
            MaximumParticipants = 10
        };

        database.Categories.Add(category);
        await database.SaveChangesAsync();

        var enrolment = new Enrolment
        {
            ParticipantID = participantId,
            EventID = raceEvent.EventID,
            CategoryID = category.CategoryID,
            EnrolmentDate = DateTime.Now,
            Status = "Confirmed"
        };

        database.Enrolments.Add(enrolment);
        await database.SaveChangesAsync();

        return enrolment.EnrolmentID;
    }

    [Fact]
    public async Task Participant_CanViewAndCancelOwnEnrolment()
    {
        using var factory = new RaceDayApiFactory();
        using var organiser = CreateClient(factory);
        using var participant = CreateClient(factory);

        int organiserId = await SignIn(
            organiser, "organiser@example.com", "Organiser");

        int participantId = await SignIn(
            participant, "participant@example.com", "Participant");

        int enrolmentId = await SeedEnrolment(
            factory, organiserId, participantId);

        var before = await participant.GetAsync("/api/enrolments/me");
        Assert.Equal(HttpStatusCode.OK, before.StatusCode);

        var entries = await before.Content
            .ReadFromJsonAsync<JsonElement>();

        Assert.Equal(1, entries.GetArrayLength());
        Assert.Equal(
            "Confirmed", entries[0].GetProperty("status").GetString());

        var cancellation = await participant.DeleteAsync(
            $"/api/enrolments/{enrolmentId}");

        Assert.Equal(HttpStatusCode.NoContent, cancellation.StatusCode);

        var after = await participant.GetFromJsonAsync<JsonElement>(
            "/api/enrolments/me");

        Assert.Equal(
            "Cancelled", after[0].GetProperty("status").GetString());
    }

    [Fact]
    public async Task Participant_CannotViewOrCancelAnotherUsersEntry()
    {
        using var factory = new RaceDayApiFactory();
        using var organiser = CreateClient(factory);
        using var owner = CreateClient(factory);
        using var other = CreateClient(factory);

        int organiserId = await SignIn(
            organiser, "organiser@example.com", "Organiser");

        int ownerId = await SignIn(
            owner, "owner@example.com", "Participant");

        await SignIn(other, "other@example.com", "Participant");

        int enrolmentId = await SeedEnrolment(
            factory, organiserId, ownerId);

        var entries = await other.GetFromJsonAsync<JsonElement>(
            "/api/enrolments/me");

        Assert.Equal(0, entries.GetArrayLength());

        var cancellation = await other.DeleteAsync(
            $"/api/enrolments/{enrolmentId}");

        Assert.Equal(HttpStatusCode.Forbidden, cancellation.StatusCode);
    }

    [Fact]
    public async Task Organiser_CannotSubmitParticipantEnrolment()
    {
        using var factory = new RaceDayApiFactory();
        using var client = CreateClient(factory);

        await SignIn(client, "organiser@example.com", "Organiser");

        var response = await client.PostAsJsonAsync(
            "/api/enrolments", new { eventID = 1, categoryID = 1 });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task CancellingMissingEnrolment_ReturnsNotFound()
    {
        using var factory = new RaceDayApiFactory();
        using var client = CreateClient(factory);

        await SignIn(client, "participant@example.com", "Participant");

        var response = await client.DeleteAsync(
            "/api/enrolments/99999");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}