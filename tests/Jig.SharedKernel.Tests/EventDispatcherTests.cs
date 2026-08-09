using Jig.SharedKernel;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using Xunit;

namespace Jig.SharedKernel.Tests;

public class EventDispatcherTests
{
    private sealed record Pinged(string What) : IIntegrationEvent;

    private sealed class Recorder : IIntegrationEventHandler<Pinged>
    {
        public List<string> Seen { get; } = [];
        public Task Handle(Pinged e, CancellationToken ct) { Seen.Add(e.What); return Task.CompletedTask; }
    }

    [Fact]
    public async Task Publish_invokes_registered_handlers()
    {
        var recorder = new Recorder();
        var sp = new ServiceCollection()
            .AddSingleton<IIntegrationEventHandler<Pinged>>(recorder)
            .BuildServiceProvider();
        IEventDispatcher dispatcher = new InProcessEventDispatcher(sp);

        await dispatcher.Publish(new Pinged("hi"), TestContext.Current.CancellationToken);

        recorder.Seen.ShouldHaveSingleItem().ShouldBe("hi");
    }
}
