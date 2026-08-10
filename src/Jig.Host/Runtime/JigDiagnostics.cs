using System.Diagnostics;

namespace Jig.Host.Runtime;

// The one ActivitySource (and, from Task 4, Meter) the app owns, both named "Jig". The pump
// opens its span from this source so a trace started by a request carries through the channel
// hand-off into the worker.
internal sealed class JigDiagnostics : IDisposable
{
    public const string SourceName = "Jig";

    public ActivitySource ActivitySource { get; } = new(SourceName);

    public void Dispose() => ActivitySource.Dispose();
}
