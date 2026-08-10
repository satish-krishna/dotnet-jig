using System.Diagnostics;
using System.Threading.Channels;
using Jig.SharedKernel;

namespace Jig.Host.Runtime;

// Drains integration events off the request thread. Each event runs in its own DI scope,
// which is what lets a module make its handler or store scoped later without the captive
// dependency the old inline dispatch would have created (see InProcessEventDispatcher).
internal sealed class EventPump(
    Channel<EventEnvelope> channel,
    IServiceScopeFactory scopeFactory,
    JigDiagnostics diagnostics,
    ILogger<EventPump> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            await foreach (var envelope in channel.Reader.ReadAllAsync(stoppingToken))
                await Dispatch(envelope);
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            // Shutdown requested. Fall through and drain what is already buffered.
        }

        // Drain events buffered when shutdown began so a rolling deploy does not drop them. The
        // host ShutdownTimeout caps how long this may take. Honest limit: hosted services stop
        // before Kestrel (LIFO registration), so an event published by a request still finishing
        // during the server's own drain can arrive after this returns and be lost. That is the
        // same non-durability this design owns on purpose; durability is a later Part's problem.
        while (channel.Reader.TryRead(out var envelope))
            await Dispatch(envelope);
    }

    private async Task Dispatch(EventEnvelope envelope)
    {
        var startedAt = Stopwatch.GetTimestamp();
        var tag = new KeyValuePair<string, object?>("event.type", envelope.EventType);

        // Linked to the publish-time context, so this span joins the request's trace.
        using var activity = diagnostics.ActivitySource.StartActivity(
            $"integration-event {envelope.EventType}", ActivityKind.Consumer, envelope.ParentContext);
        try
        {
            await using var scope = scopeFactory.CreateAsyncScope();
            await envelope.Dispatch(scope.ServiceProvider, CancellationToken.None);
            diagnostics.EventsProcessed.Add(1, tag);
        }
        catch (Exception ex)
        {
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            diagnostics.EventsFailed.Add(1, tag);
            logger.LogError(ex, "Dispatch failed for {EventType}", envelope.EventType);
        }
        finally
        {
            diagnostics.DispatchDuration.Record(Stopwatch.GetElapsedTime(startedAt).TotalMilliseconds, tag);
        }
    }
}
