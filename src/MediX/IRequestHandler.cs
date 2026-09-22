namespace MediX;

/// <summary>
/// Handler responsável por processar uma requisição do tipo <typeparamref name="TRequest"/> e
/// produzir uma resposta do tipo <typeparamref name="TResponse"/>.
/// </summary>
/// <typeparam name="TRequest">Tipo da requisição tratada por este handler.</typeparam>
/// <typeparam name="TResponse">Tipo da resposta produzida.</typeparam>
public interface IRequestHandler<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    /// <summary>Processa <paramref name="request"/> e produz a resposta correspondente.</summary>
    /// <param name="request">Requisição a ser processada.</param>
    /// <param name="cancellationToken">Token de cancelamento da operação.</param>
    Task<TResponse> HandleAsync(
        TRequest request,
        CancellationToken cancellationToken);
}
