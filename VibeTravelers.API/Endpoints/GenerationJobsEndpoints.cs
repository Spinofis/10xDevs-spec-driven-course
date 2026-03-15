using MediatR;
using Microsoft.AspNetCore.Mvc;
using VibeTravels.Application.Features.Jobs.Queries;
using VibeTravels.Application.Features.Jobs.Queries.Models;
using VibeTravelers.API;

namespace VibeTravelers.API.Endpoints;

public static class GenerationJobsEndpoints
{
    public static void MapGenerationJobsEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/generation-jobs").WithTags("GenerationJobs");

        group.MapGet("/{jobId:guid}", GetGenerationJobById)
            .WithName("GetGenerationJobById")
            .Produces<GenerationJobQueryModel>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .AllowAnonymous();
    }

    private static async Task<IResult> GetGenerationJobById(
        Guid jobId,
        [FromServices] IMediator mediator,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        var correlationId = httpContext.Request.Headers["X-Correlation-Id"].FirstOrDefault()
            ?? Guid.NewGuid().ToString();
        httpContext.Response.Headers["X-Correlation-Id"] = correlationId;

        var request = new GetGenerationJobByIdQueryRequest(jobId);
        var result = await mediator.Send(
            new GetGenerationJobByIdQuery(TripsEndpoints.DevelopmentUserId, request),
            cancellationToken);

        return result.ToHttpResult(httpContext, onSuccess: payload => Results.Ok(payload.Job));
    }
}
