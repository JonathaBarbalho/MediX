namespace MediX;

/// <summary>
/// Estratégia de execução dos handlers de uma notificação. Registrada via DI — troque a
/// implementação (ver <c>Use*NotificationPublisher</c> em <see cref="DependencyInjection"/>) para
/// mudar como <see cref="IMediator.PublishAsync{TNotification}"/> executa os handlers registrados,
/// sem alterar os call sites.
/// </summary>
public interface INotificationPublisher
{
    /// <summary>Executa <paramref name="handlers"/> para <paramref name="notification"/>.</summary>
    /// <typeparam name="TNotification">Tipo da notificação.</typeparam>
    /// <param name="handlers">Handlers registrados para o tipo da notificação, na ordem de registro.</param>
    /// <param name="notification">Notificação publicada.</param>
    /// <param name="cancellationToken">Token de cancelamento da operação.</param>
    Task PublishAsync<TNotification>(
        IReadOnlyList<INotificationHandler<TNotification>> handlers,
        TNotification notification,
        CancellationToken cancellationToken)
        where TNotification : INotification;
}
