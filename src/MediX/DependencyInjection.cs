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
        services.TryAddSingleton<INotificationPublisher, SequentialStopOnFirstExceptionPublisher>();

        var handlerInterface = typeof(IRequestHandler<,>);
        var notificationHandlerInterface = typeof(INotificationHandler<>);

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
                    if (!contract.IsGenericType)
                    {
                        continue;
                    }

                    var definition = contract.GetGenericTypeDefinition();
                    if (definition == handlerInterface || definition == notificationHandlerInterface)
                    {
                        services.AddScoped(contract, type);
                    }
                }
            }
        }

        return services;
    }

    /// <summary>
    /// Troca a estratégia de publish para <see cref="SequentialStopOnFirstExceptionPublisher"/>
    /// (o padrão já registrado por <see cref="AddMediX"/> — use este método apenas para reverter
    /// uma troca anterior de forma explícita).
    /// </summary>
    /// <param name="services">Coleção de serviços onde a troca é feita.</param>
    public static IServiceCollection UseSequentialNotificationPublisher(
        this IServiceCollection services)
    {
        services.Replace(
            ServiceDescriptor.Singleton<INotificationPublisher, SequentialStopOnFirstExceptionPublisher>());

        return services;
    }

    /// <summary>
    /// Troca a estratégia de publish para <see cref="SequentialContinueOnExceptionPublisher"/> —
    /// todos os handlers rodam mesmo que algum falhe, com as falhas agregadas ao final.
    /// </summary>
    /// <param name="services">Coleção de serviços onde a troca é feita.</param>
    public static IServiceCollection UseSequentialContinueOnExceptionNotificationPublisher(
        this IServiceCollection services)
    {
        services.Replace(
            ServiceDescriptor.Singleton<INotificationPublisher, SequentialContinueOnExceptionPublisher>());

        return services;
    }

    /// <summary>
    /// Troca a estratégia de publish para <see cref="ParallelWhenAllPublisher"/> — todos os
    /// handlers rodam em paralelo.
    /// </summary>
    /// <param name="services">Coleção de serviços onde a troca é feita.</param>
    public static IServiceCollection UseParallelNotificationPublisher(
        this IServiceCollection services)
    {
        services.Replace(
            ServiceDescriptor.Singleton<INotificationPublisher, ParallelWhenAllPublisher>());

        return services;
    }
}
