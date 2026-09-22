namespace MediX;

/// <summary>
/// Ponto único de despacho de requisições: resolve o
/// <see cref="IRequestHandler{TRequest,TResponse}"/> registrado para o tipo da requisição e o
/// invoca através da pipeline de <see cref="IPipelineBehavior{TRequest,TResponse}"/> configurada.
/// </summary>
public interface IMediator
{
    /// <summary>Despacha <paramref name="request"/> para o handler registrado e retorna a resposta.</summary>
    /// <typeparam name="TResponse">Tipo da resposta esperada.</typeparam>
    /// <param name="request">Requisição a ser despachada.</param>
    /// <param name="cancellationToken">Token de cancelamento da operação.</param>
    Task<TResponse> SendAsync<TResponse>(
        IRequest<TResponse> request,
        CancellationToken cancellationToken);
}
