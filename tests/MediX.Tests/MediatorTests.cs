using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace MediX.Tests;

public sealed class MediatorTests
{
    [Fact]
    public async Task SendAsync_WithNullRequest_ThrowsArgumentNullException()
    {
        var provider = BuildProvider();
        var mediator = provider.GetRequiredService<IMediator>();

        await Assert.ThrowsAsync<ArgumentNullException>(
            () => mediator.SendAsync<string>(null!, CancellationToken.None));
    }

    [Fact]
    public async Task SendAsync_WithoutRegisteredHandler_ThrowsInvalidOperationException()
    {
        var provider = BuildProvider();
        var mediator = provider.GetRequiredService<IMediator>();

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => mediator.SendAsync(new UnhandledPing("hi"), CancellationToken.None));

        Assert.Contains(nameof(UnhandledPing), exception.Message);
    }

    private static ServiceProvider BuildProvider()
    {
        var services = new ServiceCollection();
        services.AddMediX(typeof(MediatorTests).Assembly);

        return services.BuildServiceProvider();
    }

    public sealed record UnhandledPing(string Value) : IRequest<string>;
}
