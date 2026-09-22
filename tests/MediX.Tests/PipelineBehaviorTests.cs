using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace MediX.Tests;

public sealed class PipelineBehaviorTests
{
    [Fact]
    public async Task SendAsync_WithoutBehaviors_InvokesHandler()
    {
        var provider = BuildProvider();
        var mediator = provider.GetRequiredService<IMediator>();

        var result = await mediator.SendAsync(
            new Ping("hi"),
            CancellationToken.None);

        Assert.Equal("handled:hi", result);
    }

    [Fact]
    public async Task SendAsync_WithBehaviors_WrapsHandlerInRegistrationOrder()
    {
        var log = new List<string>();
        var provider = BuildProvider(services =>
        {
            services.AddSingleton(log);
            services.AddScoped(typeof(IPipelineBehavior<,>), typeof(FirstBehavior<,>));
            services.AddScoped(typeof(IPipelineBehavior<,>), typeof(SecondBehavior<,>));
        });
        var mediator = provider.GetRequiredService<IMediator>();

        var result = await mediator.SendAsync(
            new Ping("hi"),
            CancellationToken.None);

        Assert.Equal("handled:hi", result);
        Assert.Equal(
            new[] { "first:before", "second:before", "second:after", "first:after" },
            log);
    }

    [Fact]
    public async Task SendAsync_OnlyAppliesBehaviorWhenItChoosesTo()
    {
        var log = new List<string>();
        var provider = BuildProvider(services =>
        {
            services.AddSingleton(log);
            services.AddScoped(typeof(IPipelineBehavior<,>), typeof(MarkerOnlyBehavior<,>));
        });
        var mediator = provider.GetRequiredService<IMediator>();

        await mediator.SendAsync(new Ping("plain"), CancellationToken.None);
        await mediator.SendAsync(new MarkedPing("scoped"), CancellationToken.None);

        Assert.Equal(
            new[] { "acted:scoped" },
            log);
    }

    [Fact]
    public async Task SendAsync_WhenBehaviorInvokeReturnsNull_ThrowsInvalidOperationException()
    {
        var provider = BuildProvider(services =>
        {
            services.AddScoped(typeof(IPipelineBehavior<,>), typeof(NullReturningBehavior<,>));
        });
        var mediator = provider.GetRequiredService<IMediator>();

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => mediator.SendAsync(new Ping("hi"), CancellationToken.None));

        Assert.Contains(nameof(Ping), exception.Message);
        Assert.Contains("Behavior inválido", exception.Message);
    }

    private static ServiceProvider BuildProvider(
        Action<IServiceCollection>? configure = null)
    {
        var services = new ServiceCollection();
        services.AddMediX(typeof(PipelineBehaviorTests).Assembly);
        configure?.Invoke(services);

        return services.BuildServiceProvider();
    }

    public sealed record Ping(string Value) : IRequest<string>;

    public sealed record MarkedPing(string Value) : IRequest<string>, IMarked
    {
        public string Mark => Value;
    }

    public interface IMarked
    {
        string Mark { get; }
    }

    public sealed class PingHandler : IRequestHandler<Ping, string>
    {
        public Task<string> HandleAsync(Ping request, CancellationToken cancellationToken)
            => Task.FromResult($"handled:{request.Value}");
    }

    public sealed class MarkedPingHandler : IRequestHandler<MarkedPing, string>
    {
        public Task<string> HandleAsync(MarkedPing request, CancellationToken cancellationToken)
            => Task.FromResult($"handled:{request.Value}");
    }

    public sealed class FirstBehavior<TRequest, TResponse>(
        List<string> log) : IPipelineBehavior<TRequest, TResponse>
        where TRequest : IRequest<TResponse>
    {
        public async Task<TResponse> HandleAsync(
            TRequest request,
            RequestHandlerDelegate<TResponse> next,
            CancellationToken cancellationToken)
        {
            log.Add("first:before");
            var response = await next();
            log.Add("first:after");
            return response;
        }
    }

    public sealed class SecondBehavior<TRequest, TResponse>(
        List<string> log) : IPipelineBehavior<TRequest, TResponse>
        where TRequest : IRequest<TResponse>
    {
        public async Task<TResponse> HandleAsync(
            TRequest request,
            RequestHandlerDelegate<TResponse> next,
            CancellationToken cancellationToken)
        {
            log.Add("second:before");
            var response = await next();
            log.Add("second:after");
            return response;
        }
    }

    public sealed class MarkerOnlyBehavior<TRequest, TResponse>(
        List<string> log) : IPipelineBehavior<TRequest, TResponse>
        where TRequest : IRequest<TResponse>
    {
        public Task<TResponse> HandleAsync(
            TRequest request,
            RequestHandlerDelegate<TResponse> next,
            CancellationToken cancellationToken)
        {
            if (request is IMarked marked) {
                log.Add($"acted:{marked.Mark}");
            }

            return next();
        }
    }

    public sealed class NullReturningBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
        where TRequest : IRequest<TResponse>
    {
        public Task<TResponse> HandleAsync(
            TRequest request,
            RequestHandlerDelegate<TResponse> next,
            CancellationToken cancellationToken)
            => null!;
    }
}
