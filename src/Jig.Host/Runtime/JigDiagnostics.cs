using System.Diagnostics;
using System.Diagnostics.Metrics;
using System.Threading.Channels;
using Jig.SharedKernel;

namespace Jig.Host.Runtime;

// The one ActivitySource and Meter the app owns, both named "Jig". The pump opens its span
// from this source so a request's trace carries through the channel hand-off into the worker,
// and records the counters and histogram below. The queue-depth gauge reads the channel live.
internal sealed class JigDiagnostics : IDisposable
{
    public const string SourceName = "Jig";

    public ActivitySource ActivitySource { get; } = new(SourceName);

    private readonly Meter _meter = new(SourceName);
    public Counter<long> EventsProcessed { get; }
    public Counter<long> EventsFailed { get; }
    public Histogram<double> DispatchDuration { get; }

    public JigDiagnostics(Channel<EventEnvelope> channel)
    {
        EventsProcessed = _meter.CreateCounter<long>("jig.integration_events.processed");
        EventsFailed = _meter.CreateCounter<long>("jig.integration_events.failed");
        DispatchDuration = _meter.CreateHistogram<double>("jig.integration_events.duration", "ms");
        _meter.CreateObservableGauge("jig.integration_events.queue_depth", () => channel.Reader.Count);
    }

    public void Dispose()
    {
        ActivitySource.Dispose();
        _meter.Dispose();
    }
}
