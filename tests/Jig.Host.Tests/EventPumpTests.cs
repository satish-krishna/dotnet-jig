using System.Diagnostics;
using System.Diagnostics.Metrics;
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

    private static EventPump NewPump(Channel<EventEnvelope> channel, IServiceProvider services, JigDiagnostics diagnostics)
        => new(channel, services.GetRequiredService<IServiceScopeFactory>(), diagnostics, NullLogger<EventPump>.Instance);

    [Fact]
    public async Task Pump_runs_the_handler_off_the_publishing_thread()
    {
        var recorder = new Recorder();
        var services = new ServiceCollection()
            .AddSingleton<IIntegrationEventHandler<Pinged>>(recorder)
            .BuildServiceProvider();
        var channel = Channel.CreateBounded<EventEnvelope>(8);
        using var diagnostics = new JigDiagnostics(channel);
        var pump = NewPump(channel, services, diagnostics);
        IEventDispatcher dispatcher = new ChannelEventDispatcher(channel);

        await pump.StartAsync(TestContext.Current.CancellationToken);
        await dispatcher.Publish(new Pinged("hi"), TestContext.Current.CancellationToken);
        await recorder.Signal.Task.WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);
        await pump.StopAsync(TestContext.Current.CancellationToken);

        recorder.Seen.ShouldHaveSingleItem().ShouldBe("hi");
    }

    [Fact]
    public async Task Worker_span_joins_the_publish_time_trace()
    {
        var spans = new List<Activity>();
        using var listener = new ActivityListener
        {
            ShouldListenTo = s => s.Name == JigDiagnostics.SourceName,
            Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllData,
            ActivityStopped = spans.Add,
        };
        ActivitySource.AddActivityListener(listener);

        var requestTraceId = ActivityTraceId.CreateRandom();
        var parentSpanId = ActivitySpanId.CreateRandom();
        var parent = new ActivityContext(requestTraceId, parentSpanId, ActivityTraceFlags.Recorded);

        var recorder = new Recorder();
        var services = new ServiceCollection().BuildServiceProvider();
        var channel = Channel.CreateBounded<EventEnvelope>(8);
        using var diagnostics = new JigDiagnostics(channel);
        var pump = NewPump(channel, services, diagnostics);

        await pump.StartAsync(TestContext.Current.CancellationToken);
        await channel.Writer.WriteAsync(
            new EventEnvelope(nameof(Pinged), parent, (_, c) => recorder.Handle(new Pinged("hi"), c)),
            TestContext.Current.CancellationToken);
        await recorder.Signal.Task.WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);
        await pump.StopAsync(TestContext.Current.CancellationToken);

        var span = spans.ShouldHaveSingleItem();
        span.TraceId.ShouldBe(requestTraceId);
        span.ParentSpanId.ShouldBe(parentSpanId);
    }

    [Fact]
    public async Task Processing_an_event_increments_the_processed_counter()
    {
        long processed = 0;
        using var listener = new MeterListener
        {
            InstrumentPublished = (instrument, l) =>
            {
                if (instrument.Meter.Name == JigDiagnostics.SourceName && instrument.Name == "jig.integration_events.processed")
                    l.EnableMeasurementEvents(instrument);
            },
        };
        listener.SetMeasurementEventCallback<long>((_, measurement, _, _) => Interlocked.Add(ref processed, measurement));
        listener.Start();

        var recorder = new Recorder();
        var services = new ServiceCollection()
            .AddSingleton<IIntegrationEventHandler<Pinged>>(recorder)
            .BuildServiceProvider();
        var channel = Channel.CreateBounded<EventEnvelope>(8);
        using var diagnostics = new JigDiagnostics(channel);
        var pump = NewPump(channel, services, diagnostics);
        IEventDispatcher dispatcher = new ChannelEventDispatcher(channel);

        await pump.StartAsync(TestContext.Current.CancellationToken);
        await dispatcher.Publish(new Pinged("hi"), TestContext.Current.CancellationToken);

        var waited = Stopwatch.StartNew();
        while (Interlocked.Read(ref processed) < 1 && waited.Elapsed < TimeSpan.FromSeconds(5))
            await Task.Delay(20, TestContext.Current.CancellationToken);
        await pump.StopAsync(TestContext.Current.CancellationToken);

        Interlocked.Read(ref processed).ShouldBe(1);
    }
}
