using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace MediX.Tests;

public sealed class DependencyInjectionTests
{
    [Fact]
    public async Task AddMediX_DiscoversInternalHandlers()
    {
        var services = new ServiceCollection();
        services.AddMediX(typeof(DependencyInjectionTests).Assembly);
        var provider = services.BuildServiceProvider();
        var mediator = provider.GetRequiredService<IMediator>();

        var result = await mediator.SendAsync(new InternalPing("hi"), CancellationToken.None);

        Assert.Equal("handled-internal:hi", result);
    }

    [Fact]
    public async Task AddMediX_ScansMultipleAssemblies()
    {
        var services = new ServiceCollection();

        services.AddMediX(typeof(IMediator).Assembly, typeof(DependencyInjectionTests).Assembly);
        var provider = services.BuildServiceProvider();
        var mediator = provider.GetRequiredService<IMediator>();

        var result = await mediator.SendAsync(new MultiAssemblyPing("hi"), CancellationToken.None);

        Assert.Equal("handled-multi:hi", result);
    }

    [Fact]
    public void AddConcurrencySerialization_CalledTwice_RegistersBehaviorOnce()
    {
        var services = new ServiceCollection();
        services.AddSingleton(typeof(ILogger<>), typeof(NullLogger<>));
        services.AddMediX(typeof(DependencyInjectionTests).Assembly);

        services.AddConcurrencySerialization();
        services.AddConcurrencySerialization();

        var provider = services.BuildServiceProvider();

        var coordinators = provider.GetServices<ConcurrencyCoordinator>().ToArray();
        Assert.Single(coordinators);

        var behaviors = provider.GetServices<IPipelineBehavior<InternalPing, string>>().ToArray();
        Assert.Single(behaviors);
    }

    public sealed record InternalPing(string Value) : IRequest<string>;

    public sealed record MultiAssemblyPing(string Value) : IRequest<string>;

    public sealed class MultiAssemblyPingHandler : IRequestHandler<MultiAssemblyPing, string>
    {
        public Task<string> HandleAsync(MultiAssemblyPing request, CancellationToken cancellationToken)
            => Task.FromResult($"handled-multi:{request.Value}");
    }
}

internal sealed class InternalPingHandler : IRequestHandler<DependencyInjectionTests.InternalPing, string>
{
    public Task<string> HandleAsync(
        DependencyInjectionTests.InternalPing request,
        CancellationToken cancellationToken)
        => Task.FromResult($"handled-internal:{request.Value}");
}
