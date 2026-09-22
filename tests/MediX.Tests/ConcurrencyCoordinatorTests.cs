using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace MediX.Tests;

public sealed class ConcurrencyCoordinatorTests
{
    [Fact]
    public async Task ExecuteAsync_WhenActionThrows_LogsError()
    {
        var logger = new RecordingLogger<ConcurrencyCoordinator>();
        var coordinator = new ConcurrencyCoordinator(logger);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => coordinator.ExecuteAsync(
                "key",
                (Func<CancellationToken, Task<string>>)(_ => Task.FromException<string>(new InvalidOperationException("boom"))),
                CancellationToken.None));

        var entry = Assert.Single(logger.Entries);
        Assert.Equal(LogLevel.Error, entry.Level);
        Assert.IsType<InvalidOperationException>(entry.Exception);
        Assert.Contains("key", entry.Message);
    }

    [Fact]
    public async Task ExecuteAsync_WithAlreadyCanceledToken_ThrowsOperationCanceledException()
    {
        var coordinator = new ConcurrencyCoordinator(NullLogger<ConcurrencyCoordinator>.Instance);
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => coordinator.ExecuteAsync(
                "key",
                (Func<CancellationToken, Task<string>>)(_ => Task.FromResult("unused")),
                cts.Token));
    }

    [Fact]
    public async Task ExecuteAsync_WhenCanceledWhileWaitingForSemaphore_ThrowsWithoutBlockingOtherCallers()
    {
        var coordinator = new ConcurrencyCoordinator(NullLogger<ConcurrencyCoordinator>.Instance);
        var aEntered = new TaskCompletionSource();
        var releaseA = new TaskCompletionSource();

        var callA = coordinator.ExecuteAsync(
            "key",
            (Func<CancellationToken, Task<string>>)(async _ =>
            {
                aEntered.SetResult();
                await releaseA.Task;
                return "a";
            }),
            CancellationToken.None);

        await aEntered.Task;

        using var cts = new CancellationTokenSource();
        var callB = coordinator.ExecuteAsync(
            "key",
            (Func<CancellationToken, Task<string>>)(_ => Task.FromResult("b")),
            cts.Token);

        // Give B time to actually reach and block on the semaphore (held by A) before canceling
        // it, so the cancellation is observed while waiting — not before the call starts.
        await Task.Delay(50);
        cts.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => callB);

        releaseA.SetResult();
        Assert.Equal("a", await callA);

        var resultC = await coordinator.ExecuteAsync(
            "key",
            (Func<CancellationToken, Task<string>>)(_ => Task.FromResult("c")),
            CancellationToken.None);
        Assert.Equal("c", resultC);
    }

    private sealed class RecordingLogger<T> : ILogger<T>
    {
        public List<(LogLevel Level, Exception? Exception, string Message)> Entries { get; } = new();

        public IDisposable BeginScope<TState>(TState state) where TState : notnull => NullScope.Instance;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            Entries.Add((logLevel, exception, formatter(state, exception)));
        }

        private sealed class NullScope : IDisposable
        {
            public static readonly NullScope Instance = new();

            public void Dispose()
            {
            }
        }
    }
}
