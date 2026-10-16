using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using RaceDay.API.Data;
using Xunit;

namespace RaceDay.API.Tests;

public class AuthenticationTests
{
    private const string Password = "RaceDayDemo!2026";

    private static HttpClient CreateClient(RaceDayApiFactory factory)
    {
        return factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("https://localhost"),
            HandleCookies = true,
            AllowAutoRedirect = false
        });
    }

    private static Task<HttpResponseMessage> Register(
        HttpClient client,
        string password = Password,
        string role = "Participant")
    {
        return client.PostAsJsonAsync("/api/auth/register", new
        {
            firstName = "Test",
            lastName = "User",
            email = "test@example.com",
            password,
            role,
            phoneNumber = "0825550101"
        });
    }

    private static Task<HttpResponseMessage> Login(
        HttpClient client,
        string password = Password)
    {
        return client.PostAsJsonAsync("/api/auth/login", new
        {
            email = "test@example.com",
            password
        });
    }

    [Fact]
    public async Task Registration_HashesPassword_AndDoesNotExposeIt()
    {
        using var factory = new RaceDayApiFactory();
        using var client = CreateClient(factory);

        var response = await Register(client);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        string body = await response.Content.ReadAsStringAsync();
        Assert.DoesNotContain(Password, body);
        Assert.DoesNotContain("passwordHash", body);

        using var scope = factory.Services.CreateScope();
        var database = scope.ServiceProvider
            .GetRequiredService<RaceDayDbContext>();

        var user = await database.Users.SingleAsync();

        Assert.NotEmpty(user.PasswordHash);
        Assert.NotEqual(Password, user.PasswordHash);

        var hasher = new Microsoft.AspNetCore.Identity.PasswordHasher<
            RaceDay.API.Models.User>();

        var verification = hasher.VerifyHashedPassword(
            user, user.PasswordHash, Password);

        Assert.NotEqual(
            Microsoft.AspNetCore.Identity.PasswordVerificationResult.Failed,
            verification);
    }

    [Fact]
    public async Task DuplicateEmail_ReturnsConflict()
    {
        using var factory = new RaceDayApiFactory();
        using var client = CreateClient(factory);

        var first = await Register(client);
        Assert.Equal(HttpStatusCode.Created, first.StatusCode);

        var duplicate = await Register(client);
        Assert.Equal(HttpStatusCode.Conflict, duplicate.StatusCode);
    }

    [Theory]
    [InlineData("short", "Participant")]
    [InlineData(Password, "Administrator")]
    public async Task InvalidRegistration_ReturnsBadRequest(
        string password, string role)
    {
        using var factory = new RaceDayApiFactory();
        using var client = CreateClient(factory);

        var response = await Register(client, password, role);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task CorrectLogin_CreatesSession_ForProfileAccess()
    {
        using var factory = new RaceDayApiFactory();
        using var client = CreateClient(factory);

        var registration = await Register(client);
        Assert.Equal(HttpStatusCode.Created, registration.StatusCode);

        var login = await Login(client);
        Assert.Equal(HttpStatusCode.OK, login.StatusCode);

        var profile = await client.GetAsync("/api/profile");
        Assert.Equal(HttpStatusCode.OK, profile.StatusCode);

        string body = await profile.Content.ReadAsStringAsync();
        Assert.Contains("test@example.com", body);
        Assert.Contains("Participant", body);
    }

    [Fact]
    public async Task WrongPassword_ReturnsUnauthorized_WithoutSession()
    {
        using var factory = new RaceDayApiFactory();
        using var client = CreateClient(factory);

        var registration = await Register(client);
        Assert.Equal(HttpStatusCode.Created, registration.StatusCode);

        var login = await Login(client, "WrongPassword!2026");
        Assert.Equal(HttpStatusCode.Unauthorized, login.StatusCode);

        var profile = await client.GetAsync("/api/profile");
        Assert.Equal(HttpStatusCode.Unauthorized, profile.StatusCode);
    }

    [Fact]
    public async Task Logout_RemovesProfileAccess()
    {
        using var factory = new RaceDayApiFactory();
        using var client = CreateClient(factory);

        await Register(client);

        var login = await Login(client);
        Assert.Equal(HttpStatusCode.OK, login.StatusCode);

        var beforeLogout = await client.GetAsync("/api/profile");
        Assert.Equal(HttpStatusCode.OK, beforeLogout.StatusCode);

        var logout = await client.PostAsync("/api/auth/logout", null);
        Assert.Equal(HttpStatusCode.OK, logout.StatusCode);

        var afterLogout = await client.GetAsync("/api/profile");
        Assert.Equal(HttpStatusCode.Unauthorized, afterLogout.StatusCode);
    }

    [Fact]
    public async Task AnonymousUser_CannotAccessProfile()
    {
        using var factory = new RaceDayApiFactory();
        using var client = CreateClient(factory);

        var response = await client.GetAsync("/api/profile");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}