namespace MediX;

/// <summary>
/// Marcador para uma notificação publicada via <see cref="IMediator.PublishAsync{TNotification}"/>.
/// Ao contrário de <see cref="IRequest{TResponse}"/>, uma notificação pode ter zero, um ou vários
/// handlers — todos são executados — e não produz resposta.
/// </summary>
public interface INotification;
