using FluentValidation;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;
using VibeTravels.Application.Common.Behaviors;
using VibeTravels.Application.Features.Jobs.Services;
using VibeTravels.Application.Features.Plans.Services;

namespace VibeTravels.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        var assembly = Assembly.GetExecutingAssembly();

        services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(assembly));
        services.AddValidatorsFromAssembly(assembly);
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));
        services.AddScoped<IGenerationJobStatusMapper, GenerationJobStatusMapper>();
        services.AddScoped<ITripPlanReadService, TripPlanReadService>();

        return services;
    }
}
