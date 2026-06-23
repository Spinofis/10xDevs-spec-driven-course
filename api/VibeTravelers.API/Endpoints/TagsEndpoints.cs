using MediatR;
using Microsoft.AspNetCore.Mvc;
using VibeTravels.Application.Features.Tags.Queries;

namespace VibeTravelers.API.Endpoints;

public static class TagsEndpoints
{
    public static void MapTagsEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/tags").WithTags("Tags");

        group.MapGet("/", ListTags)
            .WithName("ListTags")
            .Produces<ListTagsQueryResponse>(StatusCodes.Status200OK)
            .AllowAnonymous();
    }

    private static async Task<IResult> ListTags(
        [FromServices] IMediator mediator,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        var correlationId = httpContext.Request.Headers["X-Correlation-Id"].FirstOrDefault()
            ?? Guid.NewGuid().ToString();
        httpContext.Response.Headers["X-Correlation-Id"] = correlationId;

        var request = new ListTagsQueryRequest();
        var response = await mediator.Send(new ListTagsQuery(request), cancellationToken);

        return Results.Ok(response);
    }
}
