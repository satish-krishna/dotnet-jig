using System.Threading.Channels;
using Jig.SharedKernel;

namespace Jig.Host.Runtime;

// Drains integration events off the request thread. Each event runs in its own DI scope,
// which is what lets a module make its handler or store scoped later without the captive
// dependency the old inline dispatch would have created (see InProcessEventDispatcher).
internal sealed class EventPump(
    Channel<EventEnvelope> channel,
    IServiceScopeFactory scopeFactory,
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
            // Normal shutdown. Task 6 adds draining of whatever is still buffered.
        }
    }

    private async Task Dispatch(EventEnvelope envelope)
    {
        try
        {
            await using var scope = scopeFactory.CreateAsyncScope();
            await envelope.Dispatch(scope.ServiceProvider, CancellationToken.None);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Dispatch failed for {EventType}", envelope.EventType);
        }
    }
}
