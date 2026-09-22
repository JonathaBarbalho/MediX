namespace MediX;

/// <summary>
/// Executa os handlers em sequência, na ordem de registro. Todos os handlers rodam mesmo que
/// algum falhe; se houver falhas, uma <see cref="AggregateException"/> reunindo todas elas é
/// lançada ao final. Habilite via <see cref="DependencyInjection.UseSequentialContinueOnExceptionNotificationPublisher"/>.
/// </summary>
public sealed class SequentialContinueOnExceptionPublisher : INotificationPublisher
{
    /// <inheritdoc/>
    public async Task PublishAsync<TNotification>(
        IReadOnlyList<INotificationHandler<TNotification>> handlers,
        TNotification notification,
        CancellationToken cancellationToken)
        where TNotification : INotification
    {
        List<Exception>? exceptions = null;

        foreach (var handler in handlers) {
            try {
                await handler.HandleAsync(notification, cancellationToken);
            }
            catch (Exception ex) {
                (exceptions ??= []).Add(ex);
            }
        }

        if (exceptions is not null) {
            throw new AggregateException(exceptions);
        }
    }
}
