using MediatR;
using Microsoft.AspNetCore.Mvc;
using VibeTravels.Application.Features.Auth.Commands;

namespace VibeTravelers.API.Endpoints;

public static class AuthEndpoints
{
    public static void MapAuthEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/auth").WithTags("Auth");

        group.MapPost("/register", Register)
            .WithName("RegisterUser")
            .Produces(StatusCodes.Status201Created)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .AllowAnonymous();
    }

    private static async Task<IResult> Register(
        [FromBody] RegisterUserCommandRequest request,
        [FromServices] IMediator mediator,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        var correlationId = httpContext.Request.Headers["X-Correlation-Id"].FirstOrDefault()
            ?? Guid.NewGuid().ToString();
        httpContext.Response.Headers["X-Correlation-Id"] = correlationId;

        var response = await mediator.Send(new RegisterUserCommand(request), cancellationToken);

        return Results.Json(new { }, statusCode: StatusCodes.Status201Created);
    }
}
