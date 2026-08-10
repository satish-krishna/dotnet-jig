using System.Threading.Channels;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using Xunit;

namespace Jig.SharedKernel.Tests;

public class ChannelEventDispatcherTests
{
    private sealed record Pinged(string What) : IIntegrationEvent;

    private sealed class Recorder : IIntegrationEventHandler<Pinged>
    {
        public List<string> Seen { get; } = [];
        public Task Handle(Pinged e, CancellationToken ct) { Seen.Add(e.What); return Task.CompletedTask; }
    }

    [Fact]
    public async Task Publish_enqueues_and_defers_the_handler_until_the_envelope_is_dispatched()
    {
        var recorder = new Recorder();
        var sp = new ServiceCollection()
            .AddSingleton<IIntegrationEventHandler<Pinged>>(recorder)
            .BuildServiceProvider();
        var channel = Channel.CreateUnbounded<EventEnvelope>();
        IEventDispatcher dispatcher = new ChannelEventDispatcher(channel);

        await dispatcher.Publish(new Pinged("hi"), TestContext.Current.CancellationToken);

        // Not run inline: the handler has seen nothing at the point Publish returns.
        recorder.Seen.ShouldBeEmpty();
        channel.Reader.TryRead(out var envelope).ShouldBeTrue();
        envelope!.EventType.ShouldBe(nameof(Pinged));

        // The captured closure resolves and runs the handler when the pump dispatches it.
        await envelope.Dispatch(sp, TestContext.Current.CancellationToken);
        recorder.Seen.ShouldHaveSingleItem().ShouldBe("hi");
    }
}
