namespace MediX;

/// <summary>
/// Executa os handlers em sequência, na ordem de registro. Se um handler falhar, a exceção é
/// propagada imediatamente e os handlers seguintes não são executados. Estratégia padrão do
/// MediX, registrada por <see cref="DependencyInjection.AddMediX"/>.
/// </summary>
public sealed class SequentialStopOnFirstExceptionPublisher : INotificationPublisher
{
    /// <inheritdoc/>
    public async Task PublishAsync<TNotification>(
        IReadOnlyList<INotificationHandler<TNotification>> handlers,
        TNotification notification,
        CancellationToken cancellationToken)
        where TNotification : INotification
    {
        foreach (var handler in handlers) {
            await handler.HandleAsync(notification, cancellationToken);
        }
    }
}
