using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace MediX;

/// <summary>Extensões de <see cref="IServiceCollection"/> para registrar o MediX e suas features opcionais.</summary>
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

    /// <summary>
    /// Registra o <see cref="IMediator"/> e, por assembly scanning, todos os
    /// <see cref="IRequestHandler{TRequest,TResponse}"/> (incluindo tipos <c>internal</c>)
    /// encontrados em <paramref name="assemblies"/>.
    /// </summary>
    /// <param name="services">Coleção de serviços onde o registro é feito.</param>
    /// <param name="assemblies">Assemblies a serem escaneados em busca de handlers.</param>
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
