using System.Threading.Channels;
using FastEndpoints;
using FastEndpoints.Swagger;
using Jig.Host;
using Jig.Host.Runtime;
using Jig.SharedKernel;
using Jig.Web;
using Scalar.AspNetCore;

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
builder.Services.AddHostedService<EventPump>();

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
app.Run();

// Exposed so the integration tests can drive the host through WebApplicationFactory.
public partial class Program;
