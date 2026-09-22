namespace MediX;

/// <summary>
/// Executa todos os handlers em paralelo. Se um ou mais handlers falharem, uma
/// <see cref="AggregateException"/> reunindo todas as falhas é lançada (diferente do
/// comportamento padrão de <c>await Task.WhenAll</c>, que propaga só a primeira). Habilite via
/// <see cref="DependencyInjection.UseParallelNotificationPublisher"/>.
/// </summary>
public sealed class ParallelWhenAllPublisher : INotificationPublisher
{
    /// <inheritdoc/>
    public async Task PublishAsync<TNotification>(
        IReadOnlyList<INotificationHandler<TNotification>> handlers,
        TNotification notification,
        CancellationToken cancellationToken)
        where TNotification : INotification
    {
        var tasks = new Task[handlers.Count];
        for (var index = 0; index < handlers.Count; index++) {
            tasks[index] = handlers[index].HandleAsync(notification, cancellationToken);
        }

        try {
            await Task.WhenAll(tasks);
        }
        catch (Exception) when (tasks.Any(task => task.IsFaulted)) {
            throw new AggregateException(
                tasks.Where(task => task.IsFaulted)
                    .SelectMany(task => task.Exception?.InnerExceptions ?? Enumerable.Empty<Exception>()));
        }
    }
}
