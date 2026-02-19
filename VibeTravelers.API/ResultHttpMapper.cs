using Microsoft.AspNetCore.Mvc;
using System.Net;
using VibeTravels.Application.Common.Results;

namespace VibeTravelers.API;

public static class ResultHttpMapper
{
    public static IResult ToHttpResult(this Result result, HttpContext httpContext, Func<IResult>? onSuccess = null)
    {
        if (result.IsSuccess)
            return onSuccess?.Invoke() ?? Results.Ok();

        return ToProblem(result.Errors, httpContext);
    }

    public static IResult ToHttpResult<T>(this Result<T> result, HttpContext httpContext, Func<T, IResult>? onSuccess = null)
    {
        if (result.IsSuccess)
        {
            if (result.Value is null)
                return onSuccess?.Invoke(default!) ?? Results.Ok();

            return onSuccess?.Invoke(result.Value) ?? Results.Ok(result.Value);
        }

        return ToProblem(result.Errors, httpContext);
    }

    private static IResult ToProblem(IReadOnlyList<Error> errors, HttpContext httpContext)
    {
        var statusCode = errors.Count > 0
            ? (int)errors.Max(e => e.Status)
            : StatusCodes.Status500InternalServerError;

        var primary = errors.Count > 0 ? errors[0] : (Error?)null;

        var extensions = new Dictionary<string, object?>
        {
            ["traceId"] = httpContext.TraceIdentifier,
            ["errors"] = errors.Select(e => new { e.Code, e.Message, e.Target, Status = (int)e.Status }).ToArray()
        };

        var correlationId = httpContext.Response.Headers["X-Correlation-Id"].FirstOrDefault()
            ?? httpContext.Request.Headers["X-Correlation-Id"].FirstOrDefault();
        if (string.IsNullOrWhiteSpace(correlationId) is false)
            extensions["correlationId"] = correlationId;

        return Results.Problem(
            title: primary?.Code ?? "ERROR",
            detail: primary?.Message ?? "Request failed.",
            statusCode: statusCode,
            instance: httpContext.Request.Path,
            extensions: extensions);
    }
}

