using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace MediX.Tests;

public sealed class ConcurrencySerializationBehaviorTests
{
    [Fact]
    public async Task SendAsync_WithSameConcurrencyKey_SerializesExecution()
    {
        var provider = BuildProvider();
        var mediator = provider.GetRequiredService<IMediator>();
        var gate = new ConcurrencyGate();

        var first = mediator.SendAsync(new ScopedPing("key", gate), CancellationToken.None);
        var second = mediator.SendAsync(new ScopedPing("key", gate), CancellationToken.None);

        await Task.WhenAll(first, second);

        Assert.Equal(1, gate.MaxConcurrent);
    }

    [Fact]
    public async Task SendAsync_WithDifferentConcurrencyKeys_ExecutesConcurrently()
    {
        var provider = BuildProvider();
        var mediator = provider.GetRequiredService<IMediator>();
        var gate = new ConcurrencyGate();

        var first = mediator.SendAsync(new ScopedPing("key-a", gate), CancellationToken.None);
        var second = mediator.SendAsync(new ScopedPing("key-b", gate), CancellationToken.None);

        await Task.WhenAll(first, second);

        Assert.Equal(2, gate.MaxConcurrent);
    }

    [Fact]
    public async Task SendAsync_WithoutConcurrencyScope_StillInvokesHandler()
    {
        var provider = BuildProvider();
        var mediator = provider.GetRequiredService<IMediator>();

        var result = await mediator.SendAsync(new PlainPing("hi"), CancellationToken.None);

        Assert.Equal("handled:hi", result);
    }

    [Fact]
    public async Task SendAsync_WhenHandlerThrows_PropagatesAndReleasesLock()
    {
        var provider = BuildProvider();
        var mediator = provider.GetRequiredService<IMediator>();

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => mediator.SendAsync(new FailingPing("boom"), CancellationToken.None));

        var resultTask = mediator.SendAsync(new FailingPing("ok"), CancellationToken.None);
        var completed = await Task.WhenAny(resultTask, Task.Delay(TimeSpan.FromSeconds(5)));

        Assert.Same(resultTask, completed);
        Assert.Equal("handled:ok", await resultTask);
    }

    private static ServiceProvider BuildProvider()
    {
        var services = new ServiceCollection();
        services.AddSingleton(typeof(ILogger<>), typeof(NullLogger<>));
        services.AddMediX(typeof(ConcurrencySerializationBehaviorTests).Assembly);
        services.AddConcurrencySerialization();

        return services.BuildServiceProvider();
    }

    public sealed class ConcurrencyGate
    {
        private int _current;
        private int _maxConcurrent;

        public int MaxConcurrent => _maxConcurrent;

        public async Task<string> RunAsync(string value)
        {
            var current = Interlocked.Increment(ref _current);
            InterlockedMax(ref _maxConcurrent, current);

            await Task.Delay(50);

            Interlocked.Decrement(ref _current);
            return $"handled:{value}";
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

    public sealed record ScopedPing(string Key, ConcurrencyGate Gate) : IRequest<string>, IConcurrencyScoped
    {
        public string ConcurrencyKey => Key;
    }

    public sealed class ScopedPingHandler : IRequestHandler<ScopedPing, string>
    {
        public Task<string> HandleAsync(ScopedPing request, CancellationToken cancellationToken)
            => request.Gate.RunAsync(request.Key);
    }

    public sealed record PlainPing(string Value) : IRequest<string>;

    public sealed class PlainPingHandler : IRequestHandler<PlainPing, string>
    {
        public Task<string> HandleAsync(PlainPing request, CancellationToken cancellationToken)
            => Task.FromResult($"handled:{request.Value}");
    }

    public sealed record FailingPing(string Value) : IRequest<string>, IConcurrencyScoped
    {
        public string ConcurrencyKey => "failing-key";
    }

    public sealed class FailingPingHandler : IRequestHandler<FailingPing, string>
    {
        public Task<string> HandleAsync(FailingPing request, CancellationToken cancellationToken)
        {
            if (request.Value == "boom") {
                return Task.FromException<string>(new InvalidOperationException("boom"));
            }

            return Task.FromResult($"handled:{request.Value}");
        }
    }
}
