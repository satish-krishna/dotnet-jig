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
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.Extensions.Options;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authentication;
using Jig.Host.Security;
using SecurityOptions = Jig.Host.Security.SecurityOptions;

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
        if (!string.IsNullOrWhiteSpace(otlpEndpoint)) t.AddOtlpExporter();
    })
    .WithMetrics(m =>
    {
        m.AddMeter(JigDiagnostics.SourceName).AddAspNetCoreInstrumentation();
        if (!string.IsNullOrWhiteSpace(otlpEndpoint)) m.AddOtlpExporter();
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

// Security. Validate-only: the app validates tokens and never issues production ones. In
// production set Authority to a real IdP and drop DevSigningKey; the handler then validates
// against the IdP's published keys instead of the symmetric dev key.
builder.Services.AddOptions<SecurityOptions>()
    .BindConfiguration("Security")
    .ValidateDataAnnotations()
    .Validate(o => !string.IsNullOrWhiteSpace(o.Authority) || o.DevSigningKey.Length >= 32,
        "Security: set Authority for production, or a DevSigningKey of at least 32 characters for the template.")
    .ValidateOnStart();
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentUser, HttpContextCurrentUser>();

// Configured from the bound options rather than an eager Configuration read, so a config override
// (a test host, an env var) is honored: the options system builds SecurityOptions from final config.
builder.Services.AddOptions<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme)
    .Configure<IOptions<SecurityOptions>>((jwt, secOptions) =>
    {
        var sec = secOptions.Value;
        jwt.MapInboundClaims = false;
        jwt.TokenValidationParameters = new TokenValidationParameters
        {
            ValidIssuer = sec.Issuer,
            ValidAudience = sec.Audience,
        };
        if (!string.IsNullOrWhiteSpace(sec.Authority))
        {
            // Production: validate against the IdP's published keys. The dev key is never a signer
            // here, so a dev key committed to config cannot forge a token in a real deployment.
            jwt.Authority = sec.Authority;
        }
        else
        {
            // Template/dev: no IdP, so validate the symmetric dev key.
            jwt.TokenValidationParameters.ValidateIssuerSigningKey = true;
            jwt.TokenValidationParameters.IssuerSigningKey =
                new SymmetricSecurityKey(Encoding.UTF8.GetBytes(sec.DevSigningKey));
        }
        // A bare 401 with an empty body would contradict every other error being ProblemDetails
        // since Part 3, so write ProblemDetails on the challenge instead.
        jwt.Events = new JwtBearerEvents
        {
            OnChallenge = async ctx =>
            {
                ctx.HandleResponse();
                ctx.Response.StatusCode = StatusCodes.Status401Unauthorized;
                var problems = ctx.HttpContext.RequestServices.GetRequiredService<IProblemDetailsService>();
                await problems.WriteAsync(new ProblemDetailsContext
                {
                    HttpContext = ctx.HttpContext,
                    ProblemDetails = { Status = StatusCodes.Status401Unauthorized, Title = "Unauthorized" },
                });
            },
        };
    });
// A forwarding scheme picks the concrete scheme by what the request carries: an API key header
// routes to the API-key handler, anything else to bearer. Both land in the same policies below.
builder.Services.AddAuthentication("Jig")
    .AddPolicyScheme("Jig", "Bearer or ApiKey", o =>
        o.ForwardDefaultSelector = ctx =>
            ctx.Request.Headers.ContainsKey(ApiKeyAuthHandler.HeaderName)
                ? ApiKeyAuthHandler.SchemeName
                : JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer()
    .AddScheme<AuthenticationSchemeOptions, ApiKeyAuthHandler>(ApiKeyAuthHandler.SchemeName, null);
builder.Services.AddAuthorization(o =>
{
    // Defined once, applied at the endpoint. A scope is a claim either scheme can carry, so the
    // same policy admits a person's token or a machine's API key without knowing which authenticated.
    o.AddPolicy("users:read", p => p.RequireAuthenticatedUser().RequireClaim("scope", "users:read"));
    o.AddPolicy("users:write", p => p.RequireAuthenticatedUser().RequireClaim("scope", "users:write"));
    // Reading one record is owner-or-admin; enumerating everyone is admin-only, so the list route
    // cannot be used to sidestep the per-record ownership rule.
    o.AddPolicy("admin", p => p.RequireAuthenticatedUser().RequireClaim("scope", "admin"));
});
builder.Services.AddSingleton<IAuthorizationMiddlewareResultHandler, ProblemDetailsAuthResultHandler>();

builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<FallbackExceptionHandler>();

var app = builder.Build();
app.UseExceptionHandler();
app.UseAuthentication();
app.UseAuthorization();
app.UseFastEndpoints(c =>
{
    c.Endpoints.RoutePrefix = "v1";
    c.Errors.UseProblemDetails();
});
app.UseSwaggerGen();
app.MapScalarApiReference(o => o.WithOpenApiRoutePattern("/swagger/v1/swagger.json"));

app.MapHealthChecks("/health/live", new HealthCheckOptions { Predicate = r => r.Tags.Contains("live") });
app.MapHealthChecks("/health/ready", new HealthCheckOptions { Predicate = r => r.Tags.Contains("ready") });

// The fourth probe in the family: /health answers "is it up", /version answers "which build is up".
// Unauthenticated and outside the versioned API on purpose, so a deploy check or a QA report reads
// it the same trivial way it reads /health, and can pin a hotfix to the exact commit that is live.
app.MapGet("/version", () => BuildInfo.Current);

app.MapDevTokenEndpoint();

app.Run();

// Exposed so the integration tests can drive the host through WebApplicationFactory.
public partial class Program;
