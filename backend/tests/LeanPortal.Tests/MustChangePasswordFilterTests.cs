using System.Security.Claims;
using LeanPortal.Api.Infrastructure;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Routing;

namespace LeanPortal.Tests;

/// <summary>
/// An account created with a temporary password must not reach the console until
/// it has been changed.
///
/// This was enforced only by the Angular router, which stops nobody who calls the
/// API with the token they were just issued - and the seeded administrator is
/// created with a password published in this repository whenever
/// Seed:AdminPassword is left unset. These cover the server-side refusal.
/// </summary>
public class MustChangePasswordFilterTests
{
    private static ActionExecutingContext ContextFor(bool mustChange, bool exempt)
    {
        var claims = new List<Claim> { new(ClaimTypes.NameIdentifier, "user-1") };
        if (mustChange) claims.Add(new Claim("must_change_password", "true"));

        var http = new DefaultHttpContext
        {
            User = new ClaimsPrincipal(new ClaimsIdentity(claims, authenticationType: "Test"))
        };
        http.Request.Path = "/api/admin/users";

        var descriptor = new ActionDescriptor { EndpointMetadata = [] };
        if (exempt) descriptor.EndpointMetadata = [new AllowPasswordChangePendingAttribute()];

        return new ActionExecutingContext(
            new ActionContext(http, new RouteData(), descriptor),
            [],
            new Dictionary<string, object?>(),
            controller: null!);
    }

    private static async Task<(bool Continued, ActionExecutingContext Context)> Run(
        bool mustChange, bool exempt)
    {
        var context = ContextFor(mustChange, exempt);
        var continued = false;

        await new MustChangePasswordFilter().OnActionExecutionAsync(context, () =>
        {
            continued = true;
            return Task.FromResult<ActionExecutedContext>(null!);
        });

        return (continued, context);
    }

    [Fact]
    public async Task Refuses_the_console_while_the_change_is_outstanding()
    {
        var (continued, context) = await Run(mustChange: true, exempt: false);

        Assert.False(continued);
        var result = Assert.IsType<ObjectResult>(context.Result);
        Assert.Equal(StatusCodes.Status403Forbidden, result.StatusCode);
    }

    [Fact]
    public async Task Allows_the_endpoints_needed_to_change_it()
    {
        // Signing out, reading who you are and setting the new password have to keep
        // working, or the account is locked out of fixing itself.
        var (continued, context) = await Run(mustChange: true, exempt: true);

        Assert.True(continued);
        Assert.Null(context.Result);
    }

    [Fact]
    public async Task Leaves_an_ordinary_session_alone()
    {
        var (continued, context) = await Run(mustChange: false, exempt: false);

        Assert.True(continued);
        Assert.Null(context.Result);
    }

    [Fact]
    public async Task Leaves_an_anonymous_request_to_the_authorization_layer()
    {
        var http = new DefaultHttpContext { User = new ClaimsPrincipal(new ClaimsIdentity()) };
        var context = new ActionExecutingContext(
            new ActionContext(http, new RouteData(), new ActionDescriptor { EndpointMetadata = [] }),
            [], new Dictionary<string, object?>(), controller: null!);

        var continued = false;
        await new MustChangePasswordFilter().OnActionExecutionAsync(context, () =>
        {
            continued = true;
            return Task.FromResult<ActionExecutedContext>(null!);
        });

        // Not this filter's job to reject it - [Authorize] answers 401 for that, and
        // answering 403 here would tell an anonymous caller the endpoint exists.
        Assert.True(continued);
    }
}
