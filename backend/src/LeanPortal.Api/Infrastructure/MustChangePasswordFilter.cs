using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace LeanPortal.Api.Infrastructure;

/// <summary>
/// Marks the few endpoints an operator may still call while their password change
/// is outstanding: reading who they are, changing the password, refreshing the
/// token that lets them do it, and signing out.
/// </summary>
[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class)]
public sealed class AllowPasswordChangePendingAttribute : Attribute;

/// <summary>
/// Refuses the console to an account that has not yet changed its password.
///
/// This existed only in the browser: the Angular router sent the operator to the
/// change-password screen, and nothing on the server cared. A guard in the browser
/// stops nobody who calls the API directly with the token they were just issued -
/// and the seeded administrator is created with a password that is published in
/// this repository, for installations where Seed:AdminPassword was never set. That
/// combination meant a known password could reach every administrative endpoint.
///
/// Applied globally rather than per controller so a new admin controller is covered
/// the day it is written, instead of the day somebody remembers.
/// </summary>
public class MustChangePasswordFilter : IAsyncActionFilter
{
    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var user = context.HttpContext.User;

        if (user.Identity?.IsAuthenticated != true
            || user.FindFirst("must_change_password")?.Value != "true")
        {
            await next();
            return;
        }

        var allowed = context.ActionDescriptor.EndpointMetadata
            .OfType<AllowPasswordChangePendingAttribute>().Any();

        if (allowed)
        {
            await next();
            return;
        }

        context.Result = new ObjectResult(new ProblemDetails
        {
            Status = StatusCodes.Status403Forbidden,
            Title = "Change your password before using the console.",
            Detail = "This account was created with a temporary password. Set a new one first.",
            Instance = context.HttpContext.Request.Path
        })
        {
            StatusCode = StatusCodes.Status403Forbidden
        };
    }
}
