namespace MediX;

/// <summary>
/// Serializa, por chave, a execução de requests marcados com <see cref="IConcurrencyScoped"/>.
/// Requests não marcados (incluindo queries) seguem direto para o próximo passo do pipeline.
/// </summary>
public sealed class ConcurrencySerializationBehavior<TRequest, TResponse>(
    ConcurrencyCoordinator coordinator) : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    public Task<TResponse> HandleAsync(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        if (request is IConcurrencyScoped scoped) {
            return coordinator.ExecuteAsync(
                scoped.ConcurrencyKey,
                _ => next(),
                cancellationToken);
        }

        return next();
    }
}
