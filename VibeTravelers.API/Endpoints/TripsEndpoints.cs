using MediatR;
using Microsoft.AspNetCore.Mvc;
using VibeTravels.Application.Features.Trips.Commands;
using VibeTravelers.API;

namespace VibeTravelers.API.Endpoints;

public static class TripsEndpoints
{
    public static readonly Guid DevelopmentUserId = Guid.Parse("11111111-1111-1111-1111-111111111111");

    public static void MapTripsEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/trips").WithTags("Trips");

        group.MapPost("/", CreateTrip)
            .WithName("CreateTrip")
            .Produces<CreateTripCommandResponse>(StatusCodes.Status201Created)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .AllowAnonymous();
    }

    private static async Task<IResult> CreateTrip(
        [FromBody] CreateTripCommandRequest request,
        [FromServices] IMediator mediator,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        var correlationId = httpContext.Request.Headers["X-Correlation-Id"].FirstOrDefault()
            ?? Guid.NewGuid().ToString();
        httpContext.Response.Headers["X-Correlation-Id"] = correlationId;

        var result = await mediator.Send(new CreateTripCommand(DevelopmentUserId, request), cancellationToken);

        return result.ToHttpResult(
            httpContext,
            onSuccess: payload => Results.Json(payload, statusCode: StatusCodes.Status201Created));
    }
}
