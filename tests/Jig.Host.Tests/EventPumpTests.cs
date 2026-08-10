using System.Threading.Channels;
using Jig.Host.Runtime;
using Jig.SharedKernel;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Shouldly;

namespace Jig.Host.Tests;

public class EventPumpTests
{
    private sealed record Pinged(string What) : IIntegrationEvent;

    private sealed class Recorder : IIntegrationEventHandler<Pinged>
    {
        public TaskCompletionSource Signal { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public List<string> Seen { get; } = [];
        public Task Handle(Pinged e, CancellationToken ct)
        {
            Seen.Add(e.What);
            Signal.TrySetResult();
            return Task.CompletedTask;
        }
    }

    [Fact]
    public async Task Pump_runs_the_handler_off_the_publishing_thread()
    {
        var recorder = new Recorder();
        var services = new ServiceCollection()
            .AddSingleton<IIntegrationEventHandler<Pinged>>(recorder)
            .BuildServiceProvider();
        var channel = Channel.CreateBounded<EventEnvelope>(8);
        var pump = new EventPump(channel, services.GetRequiredService<IServiceScopeFactory>(), NullLogger<EventPump>.Instance);
        IEventDispatcher dispatcher = new ChannelEventDispatcher(channel);

        await pump.StartAsync(TestContext.Current.CancellationToken);
        await dispatcher.Publish(new Pinged("hi"), TestContext.Current.CancellationToken);
        await recorder.Signal.Task.WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);
        await pump.StopAsync(TestContext.Current.CancellationToken);

        recorder.Seen.ShouldHaveSingleItem().ShouldBe("hi");
    }
}
