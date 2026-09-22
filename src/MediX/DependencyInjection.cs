using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace MediX;

public static class DependencyInjection
{
    /// <summary>
    /// Registra a serialização de requests marcados com <see cref="IConcurrencyScoped"/>
    /// via <see cref="ConcurrencySerializationBehavior{TRequest,TResponse}"/>.
    /// Idempotente — pode ser chamado por qualquer subsistema que use o mediator.
    /// </summary>
    public static IServiceCollection AddConcurrencySerialization(
        this IServiceCollection services)
    {
        services.TryAddSingleton<ConcurrencyCoordinator>();
        services.TryAddEnumerable(
            ServiceDescriptor.Scoped(
                typeof(IPipelineBehavior<,>),
                typeof(ConcurrencySerializationBehavior<,>)));

        return services;
    }

    public static IServiceCollection AddMediX(
        this IServiceCollection services,
        params Assembly[] assemblies)
    {
        services.AddScoped<IMediator, Mediator>();

        var handlerInterface = typeof(IRequestHandler<,>);
        foreach (var assembly in assemblies)
        {
            foreach (var type in assembly.GetTypes())
            {
                if (type.IsAbstract || type.IsInterface)
                {
                    continue;
                }

                foreach (var contract in type.GetInterfaces())
                {
                    if (contract.IsGenericType
                        && contract.GetGenericTypeDefinition() == handlerInterface)
                    {
                        services.AddScoped(contract, type);
                    }
                }
            }
        }

        return services;
    }
}
