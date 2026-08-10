namespace Jig.Host.Runtime;

// Closes the readiness gate the moment shutdown begins, before hosted services stop. That takes
// the instance out of rotation first, so new traffic stops arriving while in-flight requests and
// buffered events drain.
internal sealed class ShutdownReadiness(ReadinessGate gate, IHostApplicationLifetime lifetime) : IHostedService
{
    public Task StartAsync(CancellationToken cancellationToken)
    {
        lifetime.ApplicationStopping.Register(gate.MarkNotReady);
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
