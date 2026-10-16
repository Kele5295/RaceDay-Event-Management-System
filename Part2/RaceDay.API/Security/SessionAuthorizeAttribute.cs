using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace RaceDay.API.Security;

[AttributeUsage(
    AttributeTargets.Class | AttributeTargets.Method,
    AllowMultiple = false,
    Inherited = true)]
public class SessionAuthorizeAttribute : Attribute, IAuthorizationFilter
{
    private readonly string? _requiredRole;

    public SessionAuthorizeAttribute()
    {
    }

    public SessionAuthorizeAttribute(string requiredRole)
    {
        _requiredRole = requiredRole;
    }

    public void OnAuthorization(AuthorizationFilterContext context)
    {
        var session = context.HttpContext.Session;

        int? userId = session.GetInt32("UserID");
        string? role = session.GetString("Role");

        if (userId is null || string.IsNullOrWhiteSpace(role))
        {
            context.Result = new UnauthorizedObjectResult(new
            {
                message = "Please log in to access this resource."
            });

            return;
        }

        if (_requiredRole is not null && role != _requiredRole)
        {
            context.Result = new ObjectResult(new
            {
                message = "Your role cannot access this resource."
            })
            {
                StatusCode = StatusCodes.Status403Forbidden
            };
        }
    }
}