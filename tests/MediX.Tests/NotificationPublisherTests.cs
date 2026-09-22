using Xunit;

namespace MediX.Tests;

public sealed class NotificationPublisherTests
{
    public static IEnumerable<object[]> AllPublishers()
    {
        yield return [new SequentialStopOnFirstExceptionPublisher()];
        yield return [new SequentialContinueOnExceptionPublisher()];
        yield return [new ParallelWhenAllPublisher()];
    }

    [Theory]
    [MemberData(nameof(AllPublishers))]
    public async Task PublishAsync_WithNoHandlers_CompletesWithoutThrowing(INotificationPublisher publisher)
    {
        await publisher.PublishAsync(
            Array.Empty<INotificationHandler<PingNotification>>(),
            new PingNotification("hi"),
            CancellationToken.None);
    }

    [Fact]
    public async Task SequentialStop_RunsHandlersInOrder()
    {
        var log = new List<string>();
        INotificationHandler<PingNotification>[] handlers =
        [
            new RecordingHandler(log, "first"),
            new RecordingHandler(log, "second")
        ];

        await new SequentialStopOnFirstExceptionPublisher().PublishAsync(
            handlers, new PingNotification("hi"), CancellationToken.None);

        Assert.Equal(["first", "second"], log);
    }

    [Fact]
    public async Task SequentialStop_WhenFirstHandlerThrows_SkipsRemainingHandlersAndPropagates()
    {
        var log = new List<string>();
        INotificationHandler<PingNotification>[] handlers =
        [
            new ThrowingHandler(new InvalidOperationException("boom")),
            new RecordingHandler(log, "second")
        ];

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => new SequentialStopOnFirstExceptionPublisher().PublishAsync(
                handlers, new PingNotification("hi"), CancellationToken.None));

        Assert.Equal("boom", ex.Message);
        Assert.Empty(log);
    }

    [Fact]
    public async Task SequentialContinue_RunsAllHandlers_WhenAllSucceed()
    {
        var log = new List<string>();
        INotificationHandler<PingNotification>[] handlers =
        [
            new RecordingHandler(log, "first"),
            new RecordingHandler(log, "second")
        ];

        await new SequentialContinueOnExceptionPublisher().PublishAsync(
            handlers, new PingNotification("hi"), CancellationToken.None);

        Assert.Equal(["first", "second"], log);
    }

    [Fact]
    public async Task SequentialContinue_RunsAllHandlers_EvenWhenSomeThrow()
    {
        var log = new List<string>();
        INotificationHandler<PingNotification>[] handlers =
        [
            new ThrowingHandler(new InvalidOperationException("first-boom")),
            new RecordingHandler(log, "second"),
            new ThrowingHandler(new InvalidOperationException("third-boom"))
        ];

        var ex = await Assert.ThrowsAsync<AggregateException>(
            () => new SequentialContinueOnExceptionPublisher().PublishAsync(
                handlers, new PingNotification("hi"), CancellationToken.None));

        Assert.Equal(["second"], log);
        Assert.Equal(2, ex.InnerExceptions.Count);
        Assert.Contains(ex.InnerExceptions, inner => inner.Message == "first-boom");
        Assert.Contains(ex.InnerExceptions, inner => inner.Message == "third-boom");
    }

    [Fact]
    public async Task Parallel_RunsHandlersConcurrently()
    {
        var gate = new ConcurrencyGate();
        INotificationHandler<PingNotification>[] handlers =
        [
            new GatedHandler(gate),
            new GatedHandler(gate),
            new GatedHandler(gate)
        ];

        await new ParallelWhenAllPublisher().PublishAsync(
            handlers, new PingNotification("hi"), CancellationToken.None);

        Assert.Equal(3, gate.MaxConcurrent);
    }

    [Fact]
    public async Task Parallel_AggregatesAllFailures_NotJustTheFirst()
    {
        INotificationHandler<PingNotification>[] handlers =
        [
            new ThrowingHandler(new InvalidOperationException("first-boom")),
            new ThrowingHandler(new InvalidOperationException("second-boom"))
        ];

        var ex = await Assert.ThrowsAsync<AggregateException>(
            () => new ParallelWhenAllPublisher().PublishAsync(
                handlers, new PingNotification("hi"), CancellationToken.None));

        Assert.Equal(2, ex.InnerExceptions.Count);
    }

    [Fact]
    public async Task Parallel_WhenOnlyCanceled_PropagatesAsPureCancellation()
    {
        // Documents that pure cancellation (no real fault among the tasks) is NOT wrapped in
        // AggregateException: `catch (Exception) when (tasks.Any(t => t.IsFaulted))` does not
        // match because no task is Faulted, so the TaskCanceledException raised by
        // `await Task.WhenAll` propagates untouched.
        INotificationHandler<PingNotification>[] handlers =
        [
            new CanceledHandler()
        ];

        await Assert.ThrowsAsync<TaskCanceledException>(
            () => new ParallelWhenAllPublisher().PublishAsync(
                handlers, new PingNotification("hi"), CancellationToken.None));
    }

    [Fact]
    public async Task Parallel_WhenCanceledMixedWithRealFailure_AggregatesOnlyTheRealFailure()
    {
        // Documents that when a real fault coexists with a canceled task, Task.WhenAll surfaces
        // the Faulted status (Faulted takes priority over Canceled), the exception filter
        // matches, and the AggregateException aggregates only the IsFaulted tasks — the canceled
        // task has no `.Exception` to contribute and is excluded from the aggregation.
        INotificationHandler<PingNotification>[] handlers =
        [
            new CanceledHandler(),
            new ThrowingHandler(new InvalidOperationException("real-boom"))
        ];

        var ex = await Assert.ThrowsAsync<AggregateException>(
            () => new ParallelWhenAllPublisher().PublishAsync(
                handlers, new PingNotification("hi"), CancellationToken.None));

        var inner = Assert.Single(ex.InnerExceptions);
        Assert.Equal("real-boom", inner.Message);
    }

    public sealed record PingNotification(string Value) : INotification;

    private sealed class RecordingHandler(List<string> log, string name) : INotificationHandler<PingNotification>
    {
        public Task HandleAsync(PingNotification notification, CancellationToken cancellationToken)
        {
            log.Add(name);
            return Task.CompletedTask;
        }
    }

    private sealed class ThrowingHandler(Exception exception) : INotificationHandler<PingNotification>
    {
        public Task HandleAsync(PingNotification notification, CancellationToken cancellationToken)
            => Task.FromException(exception);
    }

    private sealed class GatedHandler(ConcurrencyGate gate) : INotificationHandler<PingNotification>
    {
        public Task HandleAsync(PingNotification notification, CancellationToken cancellationToken)
            => gate.RunAsync();
    }

    private sealed class CanceledHandler : INotificationHandler<PingNotification>
    {
        public Task HandleAsync(PingNotification notification, CancellationToken cancellationToken)
            => Task.FromCanceled(new CancellationToken(canceled: true));
    }

    private sealed class ConcurrencyGate
    {
        private int _current;
        private int _maxConcurrent;

        public int MaxConcurrent => _maxConcurrent;

        public async Task RunAsync()
        {
            var current = Interlocked.Increment(ref _current);
            InterlockedMax(ref _maxConcurrent, current);

            await Task.Delay(50);

            Interlocked.Decrement(ref _current);
        }

        private static void InterlockedMax(ref int target, int value)
        {
            int initial, computed;
            do {
                initial = target;
                computed = Math.Max(initial, value);
            } while (Interlocked.CompareExchange(ref target, computed, initial) != initial);
        }
    }
}
