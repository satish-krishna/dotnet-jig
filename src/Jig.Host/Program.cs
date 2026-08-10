using System.Threading.Channels;
using FastEndpoints;
using FastEndpoints.Swagger;
using Jig.Host;
using Jig.Host.Runtime;
using Jig.SharedKernel;
using Jig.Web;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using OpenTelemetry;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Scalar.AspNetCore;
using Serilog;
using Serilog.Sinks.OpenTelemetry;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseDefaultServiceProvider((_, options) =>
{
    options.ValidateScopes = true;
    options.ValidateOnBuild = true;
});

// Integration events are handed to a bounded channel and drained off the request thread by
// the EventPump, instead of running handlers inline on the caller. See ChannelEventDispatcher.
var eventChannel = Channel.CreateBounded<EventEnvelope>(new BoundedChannelOptions(1024)
{
    FullMode = BoundedChannelFullMode.Wait,
    SingleReader = true,
});
builder.Services.AddSingleton(eventChannel);
builder.Services.AddSingleton<IEventDispatcher, ChannelEventDispatcher>();
builder.Services.AddSingleton<JigDiagnostics>();
builder.Services.AddHostedService<EventPump>();

// Give shutdown room to drain in-flight HTTP and the pump's buffered events, and close the
// readiness gate the instant shutdown starts so no new traffic arrives during the drain.
builder.Services.Configure<HostOptions>(o => o.ShutdownTimeout = TimeSpan.FromSeconds(30));
builder.Services.AddHostedService<ShutdownReadiness>();

// The stripped host wires no telemetry, so this adds the pipeline the request and the worker
// both feed. OTLP export is added only when an endpoint is configured, so tests stay quiet and
// the compose rig (which sets OTEL_EXPORTER_OTLP_ENDPOINT) ships traces to the Aspire Dashboard.
var otlpEndpoint = builder.Configuration["OTEL_EXPORTER_OTLP_ENDPOINT"];
builder.Services.AddOpenTelemetry()
    .ConfigureResource(r => r.AddService("Jig"))
    .WithTracing(t =>
    {
        t.AddSource(JigDiagnostics.SourceName).AddAspNetCoreInstrumentation();
        if (otlpEndpoint is not null) t.AddOtlpExporter();
    })
    .WithMetrics(m =>
    {
        m.AddMeter(JigDiagnostics.SourceName).AddAspNetCoreInstrumentation();
        if (otlpEndpoint is not null) m.AddOtlpExporter();
    });

// Traces and metrics ride the OpenTelemetry listener above. Logs go through Serilog behind
// ILogger, because that is what Serilog is still for once the box produces the records: enrichment
// and, above all, sinks, which are pure configuration. The sinks sit behind an async buffer so a
// slow one never blocks the request thread (the same reason the welcome moved off it), and the
// buffer flushes on shutdown. Console always; the OTLP sink when an endpoint is set, so a log line
// lands in the same backend as its trace (the Aspire Dashboard here, Azure Monitor or Seq in
// production, which is one more WriteTo line, not a code change).
builder.Host.UseSerilog((_, sp, cfg) =>
{
    cfg.ReadFrom.Services(sp)
       .Enrich.FromLogContext()
       .WriteTo.Async(sink =>
       {
           sink.Console();
           if (!string.IsNullOrWhiteSpace(otlpEndpoint))
               sink.OpenTelemetry(o =>
               {
                   o.Endpoint = otlpEndpoint;
                   o.Protocol = OtlpProtocol.Grpc;
                   o.ResourceAttributes = new Dictionary<string, object> { ["service.name"] = "Jig" };
               });
       });
});

var moduleAssemblies = ModuleDiscovery.DiscoverAndRegister(builder.Services);

builder.Services.AddFastEndpoints(o =>
{
    o.DisableAutoDiscovery = true;
    o.IncludeAbstractValidators = true;
    o.Assemblies = moduleAssemblies;
})
.SwaggerDocument(o =>
{
    o.ShortSchemaNames = true;
    o.DocumentSettings = s =>
    {
        s.Title = "Jig API";
        s.Version = "v1";
    };
});

// Liveness answers "is the process up"; readiness answers "should traffic come here right now".
// They differ during shutdown: still live, but not ready. See ReadinessGate.
builder.Services.AddSingleton<ReadinessGate>();
builder.Services.AddHealthChecks()
    .AddCheck("self", () => HealthCheckResult.Healthy(), tags: ["live"])
    .AddCheck<ReadinessHealthCheck>("ready", tags: ["ready"]);

builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<FallbackExceptionHandler>();

var app = builder.Build();
app.UseExceptionHandler();
app.UseFastEndpoints(c =>
{
    c.Endpoints.RoutePrefix = "v1";
    c.Errors.UseProblemDetails();
});
app.UseSwaggerGen();
app.MapScalarApiReference(o => o.WithOpenApiRoutePattern("/swagger/v1/swagger.json"));

app.MapHealthChecks("/health/live", new HealthCheckOptions { Predicate = r => r.Tags.Contains("live") });
app.MapHealthChecks("/health/ready", new HealthCheckOptions { Predicate = r => r.Tags.Contains("ready") });

app.Run();

// Exposed so the integration tests can drive the host through WebApplicationFactory.
public partial class Program;
