using Microsoft.OpenApi;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace RaceDay.API.Documentation;

public class SwaggerDocumentationFilter : IOperationFilter
{
    public void Apply(
        OpenApiOperation operation,
        OperationFilterContext context)
    {
        operation.Responses ??= new OpenApiResponses();

        string controller = context.MethodInfo.DeclaringType?.Name
            .Replace("Controller", "") ?? "API";

        string method = context.ApiDescription.HttpMethod ?? "GET";
        string route = context.ApiDescription.RelativePath ?? "";

        operation.Summary = GetSummary(controller, method, route);

        var attributes = context.MethodInfo.GetCustomAttributesData()
            .AsEnumerable();

        if (context.MethodInfo.DeclaringType is not null)
        {
            attributes = attributes.Concat(
                context.MethodInfo.DeclaringType.GetCustomAttributesData());
        }

        var sessionAttribute = attributes.FirstOrDefault(a =>
            a.AttributeType.Name == "SessionAuthorizeAttribute");

        if (sessionAttribute is not null)
        {
            string role = sessionAttribute.ConstructorArguments.Count > 0
                ? sessionAttribute.ConstructorArguments[0].Value?.ToString()
                    ?? "Logged-in user"
                : "Logged-in user";

            operation.Description =
                $"Access: {role}. Log in through POST /api/auth/login "
                + "using this HTTPS browser session. "
                + "Resource ownership is checked where applicable.";

            operation.Responses.TryAdd("401", new OpenApiResponse
            {
                Description = "A valid login session is required."
            });

            operation.Responses.TryAdd("403", new OpenApiResponse
            {
                Description = "The role or resource ownership denies access."
            });
        }
        else
        {
            operation.Description =
                "Public endpoint. No login session is required.";
        }

        if (method is "POST" or "PUT")
        {
            operation.Responses.TryAdd("400", new OpenApiResponse
            {
                Description =
                    "Invalid request data or business rule violation."
            });
        }

        bool createsResource = method == "POST"
            && !route.Contains("auth/login")
            && !route.Contains("auth/logout");

        if (createsResource)
        {
            operation.Responses.Remove("200");

            operation.Responses.TryAdd("201", new OpenApiResponse
            {
                Description = "The resource was created successfully."
            });
        }

        if (method == "DELETE")
        {
            operation.Responses.Remove("200");

            operation.Responses.TryAdd("204", new OpenApiResponse
            {
                Description =
                    "The resource was deleted or enrolment cancelled."
            });
        }

        if (route.Contains("{"))
        {
            operation.Responses.TryAdd("404", new OpenApiResponse
            {
                Description = "The requested resource was not found."
            });
        }

        bool resourceController = controller is
            "Events" or "Categories" or "Enrolments" or "Results";

        bool modifyingRequest = method is "POST" or "PUT" or "DELETE";

        if (route.Contains("auth/register")
            || (resourceController && modifyingRequest))
        {
            operation.Responses.TryAdd("409", new OpenApiResponse
            {
                Description =
                    "The operation conflicts with existing records "
                    + "or capacity restrictions."
            });
        }

        if (controller == "Categories"
            && (method is "POST" or "PUT"))
        {
            operation.Description +=
                " Use maximumParticipants to set category capacity; "
                + "null means no capacity limit.";
        }

        if (controller == "Results"
            && (method is "POST" or "PUT"))
        {
            operation.Description +=
                " FinishTime must use HH:mm:ss and be below 24 hours. "
                + "The enrolment must be confirmed and the event "
                + "must not be in the future.";
        }
    }

    private static string GetSummary(
        string controller, string method, string route)
    {
        if (route.Contains("auth/register"))
        {
            return "Register an Organiser or Participant account.";
        }

        if (route.Contains("auth/login"))
        {
            return "Verify credentials and start a login session.";
        }

        if (route.Contains("auth/logout"))
        {
            return "Clear the current login session.";
        }

        if (controller == "Profile")
        {
            return method == "GET"
                ? "View your own profile."
                : "Update your own profile.";
        }

        if (controller == "Enrolments" && method == "DELETE")
        {
            return "Cancel your own enrolment while preserving its history.";
        }

        string action = method switch
        {
            "GET" => "View",
            "POST" => "Create",
            "PUT" => "Update",
            "DELETE" => "Delete",
            _ => method
        };

        return $"{action} {controller.ToLowerInvariant()}.";
    }
}