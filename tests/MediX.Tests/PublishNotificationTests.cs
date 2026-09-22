using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace MediX.Tests;

public sealed class PublishNotificationTests
{
    [Fact]
    public async Task PublishAsync_WithoutRegisteredHandlers_CompletesWithoutThrowing()
    {
        var (provider, _) = BuildProvider();
        var mediator = provider.GetRequiredService<IMediator>();

        await mediator.PublishAsync(new UnhandledNotification("hi"), CancellationToken.None);
    }

    [Fact]
    public async Task PublishAsync_WithNullNotification_ThrowsArgumentNullException()
    {
        var (provider, _) = BuildProvider();
        var mediator = provider.GetRequiredService<IMediator>();

        await Assert.ThrowsAsync<ArgumentNullException>(
            () => mediator.PublishAsync<UnhandledNotification>(null!, CancellationToken.None));
    }

    [Fact]
    public async Task PublishAsync_InvokesAllRegisteredHandlers()
    {
        var (provider, log) = BuildProvider();
        var mediator = provider.GetRequiredService<IMediator>();

        await mediator.PublishAsync(new PingNotification("hi"), CancellationToken.None);

        Assert.Equal(2, log.Count);
        Assert.Contains("first:hi", log);
        Assert.Contains("second:hi", log);
    }

    [Fact]
    public async Task PublishAsync_DiscoversInternalHandlers()
    {
        var (provider, log) = BuildProvider();
        var mediator = provider.GetRequiredService<IMediator>();

        await mediator.PublishAsync(new InternalNotification("hi"), CancellationToken.None);

        Assert.Equal(["internal:hi"], log);
    }

    [Fact]
    public async Task PublishAsync_WithDefaultPublisher_StopsOnFirstExceptionWithoutAggregating()
    {
        var (provider, _) = BuildProvider();
        var mediator = provider.GetRequiredService<IMediator>();

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => mediator.PublishAsync(new BothFailNotification("hi"), CancellationToken.None));

        Assert.True(ex.Message is "first-boom" or "second-boom");
    }

    [Fact]
    public async Task PublishAsync_AfterUseSequentialContinueOnException_AggregatesAllFailures()
    {
        var (provider, _) = BuildProvider(
            services => services.UseSequentialContinueOnExceptionNotificationPublisher());
        var mediator = provider.GetRequiredService<IMediator>();

        var ex = await Assert.ThrowsAsync<AggregateException>(
            () => mediator.PublishAsync(new BothFailNotification("hi"), CancellationToken.None));

        Assert.Equal(2, ex.InnerExceptions.Count);
    }

    [Fact]
    public async Task PublishAsync_AfterUseParallelNotificationPublisher_AggregatesAllFailures()
    {
        var (provider, _) = BuildProvider(
            services => services.UseParallelNotificationPublisher());
        var mediator = provider.GetRequiredService<IMediator>();

        var ex = await Assert.ThrowsAsync<AggregateException>(
            () => mediator.PublishAsync(new BothFailNotification("hi"), CancellationToken.None));

        Assert.Equal(2, ex.InnerExceptions.Count);
    }

    [Fact]
    public async Task PublishAsync_AfterUseSequentialNotificationPublisher_StopsOnFirstExceptionWithoutAggregating()
    {
        var (provider, _) = BuildProvider(
            services => services
                .UseSequentialContinueOnExceptionNotificationPublisher()
                .UseSequentialNotificationPublisher());
        var mediator = provider.GetRequiredService<IMediator>();

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => mediator.PublishAsync(new BothFailNotification("hi"), CancellationToken.None));

        Assert.True(ex.Message is "first-boom" or "second-boom");
    }

    private static (ServiceProvider Provider, List<string> Log) BuildProvider(
        Action<IServiceCollection>? configure = null)
    {
        var log = new List<string>();
        var services = new ServiceCollection();
        services.AddSingleton(log);
        services.AddMediX(typeof(PublishNotificationTests).Assembly);
        configure?.Invoke(services);

        return (services.BuildServiceProvider(), log);
    }

    public sealed record UnhandledNotification(string Value) : INotification;

    public sealed record PingNotification(string Value) : INotification;

    public sealed class FirstPingHandler(List<string> log) : INotificationHandler<PingNotification>
    {
        public Task HandleAsync(PingNotification notification, CancellationToken cancellationToken)
        {
            log.Add($"first:{notification.Value}");
            return Task.CompletedTask;
        }
    }

    public sealed class SecondPingHandler(List<string> log) : INotificationHandler<PingNotification>
    {
        public Task HandleAsync(PingNotification notification, CancellationToken cancellationToken)
        {
            log.Add($"second:{notification.Value}");
            return Task.CompletedTask;
        }
    }

    public sealed record InternalNotification(string Value) : INotification;

    public sealed record BothFailNotification(string Value) : INotification;

    public sealed class FirstAlwaysFailingHandler : INotificationHandler<BothFailNotification>
    {
        public Task HandleAsync(BothFailNotification notification, CancellationToken cancellationToken)
            => Task.FromException(new InvalidOperationException("first-boom"));
    }

    public sealed class SecondAlwaysFailingHandler : INotificationHandler<BothFailNotification>
    {
        public Task HandleAsync(BothFailNotification notification, CancellationToken cancellationToken)
            => Task.FromException(new InvalidOperationException("second-boom"));
    }
}

internal sealed class InternalNotificationHandler(List<string> log)
    : INotificationHandler<PublishNotificationTests.InternalNotification>
{
    public Task HandleAsync(
        PublishNotificationTests.InternalNotification notification,
        CancellationToken cancellationToken)
    {
        log.Add($"internal:{notification.Value}");
        return Task.CompletedTask;
    }
}
