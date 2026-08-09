using FastEndpoints;
using Jig.Host;
using Jig.SharedKernel;
using Jig.Web;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSingleton<IEventDispatcher, InProcessEventDispatcher>();

var moduleAssemblies = ModuleDiscovery.DiscoverAndRegister(builder.Services);

builder.Services.AddFastEndpoints(o =>
{
    o.DisableAutoDiscovery = true;
    o.IncludeAbstractValidators = true;
    o.Assemblies = moduleAssemblies;
});

builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<FallbackExceptionHandler>();

var app = builder.Build();
app.UseExceptionHandler();
app.UseFastEndpoints(c => c.Endpoints.RoutePrefix = "v1");
app.Run();
