using MediatR;
using Microsoft.AspNetCore.Mvc;
using VibeTravels.Application.Features.Jobs.Commands;
using VibeTravels.Application.Features.Jobs.Queries;
using VibeTravels.Application.Features.Plans.Commands;
using VibeTravels.Application.Features.Plans.Queries;
using VibeTravels.Application.Features.Plans.Queries.Models;
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

        group.MapGet("/{tripId:guid}", GetTripById)
            .WithName("GetTripById")
            .Produces<GetTripByIdQueryResponse>(StatusCodes.Status200OK)
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

        group.MapDelete("/{tripId:guid}", DeleteTrip)
            .WithName("DeleteTrip")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .AllowAnonymous();

        group.MapPost("/{tripId:guid}/generation-jobs", QueueGenerationJob)
            .WithName("QueueGenerationJob")
            .Produces<QueueGenerationJobCommandResponse>(StatusCodes.Status202Accepted)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .AllowAnonymous();

        group.MapGet("/{tripId:guid}/generation-jobs", ListTripGenerationJobs)
            .WithName("ListTripGenerationJobs")
            .Produces<ListTripGenerationJobsQueryResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .AllowAnonymous();

        group.MapGet("/{tripId:guid}/plan", GetPlanByTripId)
            .WithName("GetPlanByTripId")
            .Produces<PlanQueryModel>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .AllowAnonymous();

        group.MapPut("/{tripId:guid}/plan", UpdatePlan)
            .WithName("UpdatePlan")
            .Produces<PlanQueryModel>(StatusCodes.Status200OK)
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

    private static async Task<IResult> GetTripById(
        Guid tripId,
        [FromServices] IMediator mediator,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        var correlationId = httpContext.Request.Headers["X-Correlation-Id"].FirstOrDefault()
            ?? Guid.NewGuid().ToString();
        httpContext.Response.Headers["X-Correlation-Id"] = correlationId;

        var request = new GetTripByIdQueryRequest(tripId);
        var result = await mediator.Send(new GetTripByIdQuery(DevelopmentUserId, request), cancellationToken);

        return result.ToHttpResult(httpContext, onSuccess: payload => Results.Ok(payload));
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

    private static async Task<IResult> DeleteTrip(
        Guid tripId,
        [FromServices] IMediator mediator,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        var correlationId = httpContext.Request.Headers["X-Correlation-Id"].FirstOrDefault()
            ?? Guid.NewGuid().ToString();
        httpContext.Response.Headers["X-Correlation-Id"] = correlationId;

        var request = new DeleteTripCommandRequest(tripId);
        var result = await mediator.Send(new DeleteTripCommand(DevelopmentUserId, request), cancellationToken);

        return result.ToHttpResult(httpContext, onSuccess: Results.NoContent);
    }

    private static async Task<IResult> QueueGenerationJob(
        Guid tripId,
        [FromServices] IMediator mediator,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        var correlationId = httpContext.Request.Headers["X-Correlation-Id"].FirstOrDefault()
            ?? Guid.NewGuid().ToString();
        httpContext.Response.Headers["X-Correlation-Id"] = correlationId;

        var request = new QueueGenerationJobCommandRequest(tripId);
        var result = await mediator.Send(new QueueGenerationJobCommand(DevelopmentUserId, request), cancellationToken);

        return result.ToHttpResult(
            httpContext,
            onSuccess: payload => Results.Json(payload, statusCode: StatusCodes.Status202Accepted));
    }

    private static async Task<IResult> ListTripGenerationJobs(
        Guid tripId,
        [FromQuery(Name = "limit")] int? limit,
        [FromQuery(Name = "cursor")] string? cursor,
        [FromServices] IMediator mediator,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        var correlationId = httpContext.Request.Headers["X-Correlation-Id"].FirstOrDefault()
            ?? Guid.NewGuid().ToString();
        httpContext.Response.Headers["X-Correlation-Id"] = correlationId;

        var request = new ListTripGenerationJobsQueryRequest(
            TripId: tripId,
            Limit: limit,
            Cursor: cursor);

        var result = await mediator.Send(
            new ListTripGenerationJobsQuery(DevelopmentUserId, request),
            cancellationToken);

        return result.ToHttpResult(httpContext, onSuccess: payload => Results.Ok(payload));
    }

    private static async Task<IResult> GetPlanByTripId(
        Guid tripId,
        [FromServices] IMediator mediator,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        var correlationId = httpContext.Request.Headers["X-Correlation-Id"].FirstOrDefault()
            ?? Guid.NewGuid().ToString();
        httpContext.Response.Headers["X-Correlation-Id"] = correlationId;

        var request = new GetPlanByTripIdQueryRequest(tripId);
        var result = await mediator.Send(
            new GetPlanByTripIdQuery(DevelopmentUserId, request),
            cancellationToken);

        return result.ToHttpResult(httpContext, onSuccess: payload => Results.Ok(payload.Plan));
    }

    private static async Task<IResult> UpdatePlan(
        Guid tripId,
        [FromBody] UpdatePlanCommandRequest request,
        [FromServices] IMediator mediator,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        var correlationId = httpContext.Request.Headers["X-Correlation-Id"].FirstOrDefault()
            ?? Guid.NewGuid().ToString();
        httpContext.Response.Headers["X-Correlation-Id"] = correlationId;

        var commandRequest = request with { TripId = tripId };
        var result = await mediator.Send(new UpdatePlanCommand(DevelopmentUserId, commandRequest), cancellationToken);

        return result.ToHttpResult(httpContext, onSuccess: payload => Results.Ok(payload.Plan));
    }
}
