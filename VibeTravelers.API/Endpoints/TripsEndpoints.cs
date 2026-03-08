using MediatR;
using Microsoft.AspNetCore.Mvc;
using VibeTravels.Application.Features.Trips.Commands;
using VibeTravels.Application.Features.Trips.Queries;
using VibeTravelers.API;

namespace VibeTravelers.API.Endpoints;

public static class TripsEndpoints
{
    public static readonly Guid DevelopmentUserId = Guid.Parse("11111111-1111-1111-1111-111111111111");

    public static void MapTripsEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/trips").WithTags("Trips");

        group.MapGet("/", ListTrips)
            .WithName("ListTrips")
            .Produces<ListTripsQueryResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .AllowAnonymous();

        group.MapPost("/", CreateTrip)
            .WithName("CreateTrip")
            .Produces<CreateTripCommandResponse>(StatusCodes.Status201Created)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .AllowAnonymous();

        group.MapPatch("/{tripId:guid}", PatchTrip)
            .WithName("PatchTrip")
            .Produces<PatchTripCommandResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .AllowAnonymous();
    }

    private static async Task<IResult> ListTrips(
        [FromQuery(Name = "q")] string? q,
        [FromQuery(Name = "hasPlan")] bool? hasPlan,
        [FromQuery(Name = "includeDeleted")] bool? includeDeleted,
        [FromQuery(Name = "limit")] int? limit,
        [FromQuery(Name = "cursor")] string? cursor,
        [FromQuery(Name = "sort")] string? sort,
        [FromServices] IMediator mediator,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        var correlationId = httpContext.Request.Headers["X-Correlation-Id"].FirstOrDefault()
            ?? Guid.NewGuid().ToString();
        httpContext.Response.Headers["X-Correlation-Id"] = correlationId;

        var request = new ListTripsQueryRequest(
            Query: q,
            HasPlan: hasPlan,
            IncludeDeleted: includeDeleted,
            Limit: limit,
            Cursor: cursor,
            Sort: sort);

        var result = await mediator.Send(new ListTripsQuery(DevelopmentUserId, request), cancellationToken);

        return result.ToHttpResult(httpContext, onSuccess: payload => Results.Ok(payload));
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

    private static async Task<IResult> PatchTrip(
        Guid tripId,
        [FromBody] PatchTripCommandRequest request,
        [FromServices] IMediator mediator,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        var correlationId = httpContext.Request.Headers["X-Correlation-Id"].FirstOrDefault()
            ?? Guid.NewGuid().ToString();
        httpContext.Response.Headers["X-Correlation-Id"] = correlationId;

        var result = await mediator.Send(
            new PatchTripCommand
            {
                UserId = DevelopmentUserId,
                TripId = tripId,
                Request = request
            },
            cancellationToken);

        return result.ToHttpResult(httpContext, onSuccess: payload => Results.Ok(payload));
    }
}
