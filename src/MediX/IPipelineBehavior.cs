namespace MediX;

/// <summary>Delegate que invoca o próximo passo da pipeline (o próximo behavior, ou o handler final).</summary>
/// <typeparam name="TResponse">Tipo da resposta produzida.</typeparam>
public delegate Task<TResponse> RequestHandlerDelegate<TResponse>();

/// <summary>
/// Intercepta o envio de uma requisição antes e/ou depois da execução do
/// <see cref="IRequestHandler{TRequest,TResponse}"/> correspondente. Múltiplos behaviors
/// registrados para o mesmo tipo de requisição são encadeados na ordem de registro.
/// </summary>
/// <typeparam name="TRequest">Tipo da requisição interceptada.</typeparam>
/// <typeparam name="TResponse">Tipo da resposta produzida.</typeparam>
public interface IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    /// <summary>Executa a lógica do behavior em torno de <paramref name="next"/>.</summary>
    /// <param name="request">Requisição em processamento.</param>
    /// <param name="next">Delegate que invoca o próximo passo da pipeline.</param>
    /// <param name="cancellationToken">Token de cancelamento da operação.</param>
    Task<TResponse> HandleAsync(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken);
}
