using LeanPortal.Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc;

namespace LeanPortal.Api.Controllers;

[ApiController]
[Produces("application/json")]
public abstract class ApiControllerBase(ApplicationDbContext db) : ControllerBase
{
    protected ApplicationDbContext Db { get; } = db;

    /// <summary>404 with a consistent problem-details body.</summary>
    protected ActionResult NotFoundProblem(string what) =>
        Problem(statusCode: StatusCodes.Status404NotFound, title: $"{what} was not found.", instance: Request.Path);

    /// <summary>400 with a consistent problem-details body.</summary>
    protected ActionResult BadRequestProblem(string message) =>
        Problem(statusCode: StatusCodes.Status400BadRequest, title: message, instance: Request.Path);

    /// <summary>409 used when a delete is blocked by references from other content.</summary>
    protected ActionResult ConflictProblem(string message) =>
        Problem(statusCode: StatusCodes.Status409Conflict, title: message, instance: Request.Path);

    protected string? ClientIp => HttpContext.Connection.RemoteIpAddress?.ToString();
}
