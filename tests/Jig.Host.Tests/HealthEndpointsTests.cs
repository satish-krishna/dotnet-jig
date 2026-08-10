using System.Net;
using Jig.Host.Runtime;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;

namespace Jig.Host.Tests;

public class HealthEndpointsTests
{
    [Fact]
    public async Task Live_and_ready_are_both_healthy_at_rest()
    {
        await using var factory = new JigApiFactory();
        var client = factory.CreateClient();

        var live = await client.GetAsync("/health/live", TestContext.Current.CancellationToken);
        var ready = await client.GetAsync("/health/ready", TestContext.Current.CancellationToken);

        live.StatusCode.ShouldBe(HttpStatusCode.OK);
        ready.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Ready_reports_unhealthy_once_the_gate_is_closed()
    {
        await using var factory = new JigApiFactory();
        var client = factory.CreateClient();
        factory.Services.GetRequiredService<ReadinessGate>().MarkNotReady();

        var live = await client.GetAsync("/health/live", TestContext.Current.CancellationToken);
        var ready = await client.GetAsync("/health/ready", TestContext.Current.CancellationToken);

        // Still alive, so the process must not be killed; just not taking new traffic.
        live.StatusCode.ShouldBe(HttpStatusCode.OK);
        ready.StatusCode.ShouldBe(HttpStatusCode.ServiceUnavailable);
    }
}
