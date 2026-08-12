using System.Reflection;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Sprout.Application.Common.Behaviours;
using Sprout.Application.Common.Services;

namespace Sprout.Application;

public static class DependencyInjection
{
    /// <summary>
    /// Registers every MediatR handler and FluentValidation validator in this assembly,
    /// plus the pipeline: logging on the outside, validation just inside it, so a bad
    /// request is still logged but never reaches a handler.
    /// </summary>
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        var assembly = Assembly.GetExecutingAssembly();

        services.AddMediatR(cfg =>
        {
            cfg.RegisterServicesFromAssembly(assembly);
            cfg.AddOpenBehavior(typeof(LoggingBehaviour<,>));
            cfg.AddOpenBehavior(typeof(ValidationBehaviour<,>));
        });

        services.AddValidatorsFromAssembly(assembly, includeInternalTypes: true);
        services.AddScoped<ListAccess>();

        return services;
    }
}
