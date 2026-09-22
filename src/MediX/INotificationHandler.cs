namespace MediX;

/// <summary>
/// Handler que reage a uma notificação do tipo <typeparamref name="TNotification"/>. Múltiplos
/// handlers podem existir para o mesmo tipo de notificação; todos são executados quando ela é
/// publicada, conforme a estratégia de <see cref="INotificationPublisher"/> configurada.
/// </summary>
/// <typeparam name="TNotification">Tipo da notificação tratada por este handler.</typeparam>
public interface INotificationHandler<TNotification>
    where TNotification : INotification
{
    /// <summary>Processa <paramref name="notification"/>.</summary>
    /// <param name="notification">Notificação publicada.</param>
    /// <param name="cancellationToken">Token de cancelamento da operação.</param>
    Task HandleAsync(
        TNotification notification,
        CancellationToken cancellationToken);
}
