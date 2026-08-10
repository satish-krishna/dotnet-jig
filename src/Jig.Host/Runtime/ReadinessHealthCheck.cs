using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Jig.Host.Runtime;

internal sealed class ReadinessHealthCheck(ReadinessGate gate) : IHealthCheck
{
    public Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
        => Task.FromResult(gate.IsReady
            ? HealthCheckResult.Healthy()
            : HealthCheckResult.Unhealthy("shutting down"));
}
