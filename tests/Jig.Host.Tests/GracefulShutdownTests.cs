using System.Threading.Channels;
using Jig.Host.Runtime;
using Jig.SharedKernel;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Abstractions;
using Shouldly;

namespace Jig.Host.Tests;

public class GracefulShutdownTests
{
    private sealed record Pinged(string What) : IIntegrationEvent;

    // Blocks the first event it handles until released, so later events stay buffered while the
    // pump is occupied. Every event, including the blocked one, is counted once it completes.
    private sealed class BlockingHandler(TaskCompletionSource started, Task release, Action onHandled)
        : IIntegrationEventHandler<Pinged>
    {
        private int _calls;
        public async Task Handle(Pinged e, CancellationToken ct)
        {
            if (Interlocked.Increment(ref _calls) == 1)
            {
                started.TrySetResult();
                await release;
            }
            onHandled();
        }
    }

    private sealed class TestLifetime : IHostApplicationLifetime, IDisposable
    {
        private readonly CancellationTokenSource _stopping = new();
        public CancellationToken ApplicationStarted => CancellationToken.None;
        public CancellationToken ApplicationStopping => _stopping.Token;
        public CancellationToken ApplicationStopped => CancellationToken.None;
        public void StopApplication() => _stopping.Cancel();
        public void Dispose() => _stopping.Dispose();
    }

    [Fact]
    public async Task Shutdown_closes_the_readiness_gate()
    {
        var gate = new ReadinessGate();
        using var lifetime = new TestLifetime();
        var hook = new ShutdownReadiness(gate, lifetime);
        await hook.StartAsync(TestContext.Current.CancellationToken);

        gate.IsReady.ShouldBeTrue();
        lifetime.StopApplication();
        gate.IsReady.ShouldBeFalse();
    }

    [Fact]
    public async Task Buffered_events_drain_on_stop_instead_of_being_dropped()
    {
        var started = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var handled = 0;
        var handler = new BlockingHandler(started, release.Task, () => Interlocked.Increment(ref handled));

        var services = new ServiceCollection()
            .AddSingleton<IIntegrationEventHandler<Pinged>>(handler)
            .BuildServiceProvider();
        var channel = Channel.CreateBounded<EventEnvelope>(16);
        using var diagnostics = new JigDiagnostics(channel);
        var pump = new EventPump(channel, services.GetRequiredService<IServiceScopeFactory>(), diagnostics, NullLogger<EventPump>.Instance);
        IEventDispatcher dispatcher = new ChannelEventDispatcher(channel);

        await pump.StartAsync(TestContext.Current.CancellationToken);
        for (var i = 0; i < 3; i++)
            await dispatcher.Publish(new Pinged($"e{i}"), TestContext.Current.CancellationToken);

        // The pump is now blocked inside the first event's handler; the other two are buffered.
        await started.Task.WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);

        var stopping = pump.StopAsync(TestContext.Current.CancellationToken);
        release.SetResult();
        await stopping.WaitAsync(TimeSpan.FromSeconds(10), TestContext.Current.CancellationToken);

        Volatile.Read(ref handled).ShouldBe(3);
    }
}
