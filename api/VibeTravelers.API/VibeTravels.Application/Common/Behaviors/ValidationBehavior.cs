using FluentValidation;
using MediatR;
using VibeTravels.Domain.Common.Results;

namespace VibeTravels.Application.Common.Behaviors;

public sealed class ValidationBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    private readonly IEnumerable<IValidator<TRequest>> _validators;

    public ValidationBehavior(IEnumerable<IValidator<TRequest>> validators)
    {
        _validators = validators;
    }

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        if (_validators.Any() is false)
            return await next();

        if (typeof(Result).IsAssignableFrom(typeof(TResponse)) is false)
            return await next();

        var context = new ValidationContext<TRequest>(request);
        var results = await Task.WhenAll(_validators.Select(v => v.ValidateAsync(context, cancellationToken)));

        var failures = results
            .SelectMany(r => r.Errors)
            .Where(f => f is not null)
            .ToList();

        if (failures.Count == 0)
            return await next();

        var errors = failures
            .Select(f => ResultErrors.Validation(f.ErrorMessage, NormalizeTarget(f.PropertyName)))
            .ToList();

        return CreateFailureResponse(errors);
    }

    private static string? NormalizeTarget(string? propertyName)
    {
        if (string.IsNullOrWhiteSpace(propertyName))
            return null;

        var lastDot = propertyName.LastIndexOf('.');
        return lastDot >= 0 ? propertyName[(lastDot + 1)..] : propertyName;
    }

    private static TResponse CreateFailureResponse(IReadOnlyList<Error> errors)
    {
        if (typeof(TResponse) == typeof(Result))
            return (TResponse)(object)Result.Fail(errors);

        if (typeof(TResponse).IsGenericType is false)
            throw new InvalidOperationException($"ValidationBehavior supports Result/Result<T> responses only. Got '{typeof(TResponse).Name}'.");

        if (typeof(TResponse).GetGenericTypeDefinition() != typeof(Result<>))
            throw new InvalidOperationException($"ValidationBehavior supports Result/Result<T> responses only. Got '{typeof(TResponse).Name}'.");

        var failMethod = typeof(TResponse)
            .GetMethods(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static)
            .SingleOrDefault(m =>
                m.Name == "Fail"
                && m.GetParameters().Length == 1
                && m.GetParameters()[0].ParameterType == typeof(IReadOnlyList<Error>));

        if (failMethod is null)
            throw new InvalidOperationException($"Could not find static Fail(IReadOnlyList<Error>) on '{typeof(TResponse).Name}'.");

        return (TResponse)failMethod.Invoke(null, new object?[] { errors })!;
    }
}
