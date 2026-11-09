using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace RaceDay.MVC.Services;

public sealed record ApiResponse<T>(
    bool Success,
    T? Data,
    string Error,
    int StatusCode);

public class RaceDayApiClient(
    IHttpClientFactory factory,
    IHttpContextAccessor accessor)
{
    private const string CookieKey = "ApiSessionCookie";

    public async Task<ApiResponse<T>> SendAsync<T>(
        HttpMethod method,
        string route,
        object? body = null)
    {
        using var request = new HttpRequestMessage(method, route);

        if (body is not null)
        {
            request.Content = JsonContent.Create(body);
        }

        var session = accessor.HttpContext!.Session;
        string? cookie = session.GetString(CookieKey);

        if (!string.IsNullOrEmpty(cookie))
        {
            request.Headers.TryAddWithoutValidation("Cookie", cookie);
        }

        try
        {
            var client = factory.CreateClient("RaceDayAPI");
            using var response = await client.SendAsync(request);

            if (route.Trim('/').Equals(
                "api/auth/login", StringComparison.OrdinalIgnoreCase))
            {
                if (response.IsSuccessStatusCode)
                {
                    session.Clear();

                    if (response.Headers.TryGetValues(
                        "Set-Cookie", out var cookies))
                    {
                        string? apiCookie = cookies
                            .Select(value => value.Split(';')[0])
                            .FirstOrDefault(value => value.StartsWith(
                                ".RaceDay.Session=",
                                StringComparison.Ordinal));

                        if (apiCookie is not null)
                        {
                            session.SetString(CookieKey, apiCookie);
                        }
                    }
                }
            }

            if (response.StatusCode == HttpStatusCode.Unauthorized)
            {
                session.Clear();
            }

            if (!response.IsSuccessStatusCode)
            {
                string error = await ReadErrorAsync(response);

                return new(
                    false, default, error, (int)response.StatusCode);
            }

            if (response.StatusCode == HttpStatusCode.NoContent)
            {
                return new(true, default, "", (int)response.StatusCode);
            }

            T? data = await response.Content.ReadFromJsonAsync<T>();

            return new(true, data, "", (int)response.StatusCode);
        }
        catch (HttpRequestException)
        {
            return new(
                false, default,
                "Cannot reach RaceDay API. Please try again shortly.",
                503);
        }
        catch (TaskCanceledException)
        {
            return new(
                false, default,
                "The API took too long to respond. Please try again.",
                504);
        }
        catch (JsonException)
        {
            return new(
                false, default,
                "The API returned an unexpected response.",
                502);
        }
    }

    private static async Task<string> ReadErrorAsync(
        HttpResponseMessage response)
    {
        try
        {
            var data = await response.Content
                .ReadFromJsonAsync<JsonElement>();

            if (data.TryGetProperty("message", out var message))
            {
                return message.GetString() ?? "Request failed.";
            }

            if (data.TryGetProperty("errors", out var errors))
            {
                var messages = errors.EnumerateObject()
                    .SelectMany(property => property.Value.EnumerateArray())
                    .Select(value => value.GetString())
                    .Where(value => !string.IsNullOrWhiteSpace(value));

                return string.Join(" ", messages);
            }
        }
        catch (JsonException)
        {
            // Use a friendly fallback instead of displaying raw errors.
        }

        return response.StatusCode switch
        {
            HttpStatusCode.Unauthorized => "Please log in to continue.",
            HttpStatusCode.Forbidden =>
                "You do not have permission to perform this action.",
            HttpStatusCode.NotFound => "The requested item was not found.",
            _ => "The request could not be completed."
        };
    }
}