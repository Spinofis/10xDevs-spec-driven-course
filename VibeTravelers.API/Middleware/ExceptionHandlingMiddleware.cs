using System.Net;
using System.Text.Json;
using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using VibeTravels.Application.Common.Errors;

namespace VibeTravelers.API.Middleware;

public sealed class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

    public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(context, ex);
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception ex)
    {
        var (statusCode, code) = ex switch
        {
            AppException appEx => (
                appEx.StatusCode,
                appEx.ErrorCode),
            ValidationException => (
                HttpStatusCode.BadRequest,
                ValidationErrorException.ErrorCode),
            _ => (
                HttpStatusCode.InternalServerError,
                "INTERNAL_ERROR")
        };

        if ((int)statusCode >= 500)
            _logger.LogError(ex, "Unhandled exception: {Message}", ex.Message);
        else
            _logger.LogDebug(ex, "Client error: {Code} - {Message}", code, ex.Message);

        context.Response.StatusCode = (int)statusCode;
        context.Response.ContentType = "application/problem+json";

        var problem = new ProblemDetails
        {
            Status = (int)statusCode,
            Title = code,
            Detail = ex.Message,
            Instance = context.Request.Path
        };

        if (ex is ValidationException validationEx && validationEx.Errors.Any())
        {
            problem.Extensions["errors"] = validationEx.Errors
                .GroupBy(e => e.PropertyName)
                .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray());
        }

        var traceId = context.TraceIdentifier;
        problem.Extensions["traceId"] = traceId;

        await context.Response.WriteAsync(JsonSerializer.Serialize(problem, JsonOptions));
    }
}
