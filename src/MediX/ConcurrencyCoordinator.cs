using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;

namespace MediX;

/// <summary>
/// Serializa a execução de ações por chave: chamadas com a mesma chave se aguardam por um
/// semáforo dedicado; chaves diferentes executam em paralelo.
/// </summary>
public sealed class ConcurrencyCoordinator(ILogger<ConcurrencyCoordinator> logger)
{
    private readonly ConcurrentDictionary<string, SemaphoreSlim> _locks = new();

    public async Task<T> ExecuteAsync<T>(
        string key,
        Func<CancellationToken, Task<T>> action,
        CancellationToken cancellationToken = default)
    {
        var semaphore = _locks.GetOrAdd(key, _ => new SemaphoreSlim(1, 1));
        await semaphore.WaitAsync(cancellationToken);

        try {
            return await action(cancellationToken);
        }
        catch (Exception ex) {
            logger.LogError(ex, "Concurrency-scoped execution failed for key '{Key}'.", key);
            throw;
        }
        finally {
            semaphore.Release();
        }
    }
}
