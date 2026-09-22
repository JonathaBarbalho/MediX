using Microsoft.Extensions.DependencyInjection;

namespace MediX;

internal sealed class Mediator(
    IServiceProvider serviceProvider) : IMediator {
    public Task<TResponse> SendAsync<TResponse>(
        IRequest<TResponse> request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var requestType = request.GetType();

        var handlerType = typeof(IRequestHandler<,>).MakeGenericType(
            requestType,
            typeof(TResponse));
        var handler = serviceProvider.GetService(handlerType)
            ?? throw new InvalidOperationException($"Handler não registrado para {requestType.Name}.");

        var handleMethod = handlerType.GetMethod(nameof(IRequestHandler<IRequest<TResponse>, TResponse>.HandleAsync))
            ?? throw new InvalidOperationException($"Handler inválido para {requestType.Name}.");

        RequestHandlerDelegate<TResponse> pipeline = () => {
            var response = handleMethod.Invoke(
                handler,
                [
                    request,
                    cancellationToken
                ]) ?? throw new InvalidOperationException($"Handler inválido para {requestType.Name}.");

            return (Task<TResponse>)response;
        };

        var behaviorType = typeof(IPipelineBehavior<,>).MakeGenericType(
            requestType,
            typeof(TResponse));
        var behaviors = serviceProvider.GetServices(behaviorType)
            .Where(behavior => behavior is not null)
            .ToArray();

        if (behaviors.Length == 0) {
            return pipeline();
        }

        var behaviorHandleMethod = behaviorType.GetMethod(
                nameof(IPipelineBehavior<IRequest<TResponse>, TResponse>.HandleAsync))
            ?? throw new InvalidOperationException($"Behavior inválido para {requestType.Name}.");

        for (var index = behaviors.Length - 1; index >= 0; index--) {
            var behavior = behaviors[index];
            var next = pipeline;

            pipeline = () => {
                var response = behaviorHandleMethod.Invoke(
                    behavior,
                    [
                        request,
                        next,
                        cancellationToken
                    ]) ?? throw new InvalidOperationException($"Behavior inválido para {requestType.Name}.");

                return (Task<TResponse>)response;
            };
        }

        return pipeline();
    }

    public Task PublishAsync<TNotification>(
        TNotification notification,
        CancellationToken cancellationToken)
        where TNotification : INotification
    {
        ArgumentNullException.ThrowIfNull(notification);

        var handlers = serviceProvider.GetServices<INotificationHandler<TNotification>>().ToList();
        var publisher = serviceProvider.GetRequiredService<INotificationPublisher>();

        return publisher.PublishAsync(handlers, notification, cancellationToken);
    }
}
