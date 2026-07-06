using MediatR;
using Microsoft.AspNetCore.Mvc;
using VibeTravels.Application.Features.Me.Commands;
using VibeTravels.Application.Features.Me.Queries;
using VibeTravels.Application.Features.Me.Queries.Models;
using VibeTravelers.API;

namespace VibeTravelers.API.Endpoints;

public static class MeEndpoints
{
    public static void MapMeEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/me").WithTags("Me");

        group.MapGet("/profile", GetProfile)
            .WithName("GetUserProfile")
            .Produces<GetUserProfileQueryResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .AllowAnonymous();

        group.MapPut("/profile", UpsertProfile)
            .WithName("UpsertUserProfile")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .AllowAnonymous();
    }

    private static async Task<IResult> GetProfile(
        [FromServices] IMediator mediator,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        var correlationId = httpContext.Request.Headers["X-Correlation-Id"].FirstOrDefault()
            ?? Guid.NewGuid().ToString();
        httpContext.Response.Headers["X-Correlation-Id"] = correlationId;

        var result = await mediator.Send(
            new GetUserProfileQuery(TripsEndpoints.DevelopmentUserId, new GetUserProfileQueryRequest()),
            cancellationToken);

        return result.ToHttpResult(httpContext, onSuccess: payload => Results.Ok(payload));
    }

    private static async Task<IResult> UpsertProfile(
        [FromBody] UpsertUserProfileCommandRequest request,
        [FromServices] IMediator mediator,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        var correlationId = httpContext.Request.Headers["X-Correlation-Id"].FirstOrDefault()
            ?? Guid.NewGuid().ToString();
        httpContext.Response.Headers["X-Correlation-Id"] = correlationId;

        var result = await mediator.Send(
            new UpsertUserProfileCommand(TripsEndpoints.DevelopmentUserId, request),
            cancellationToken);

        return result.ToHttpResult(httpContext, onSuccess: Results.NoContent);
    }
}
