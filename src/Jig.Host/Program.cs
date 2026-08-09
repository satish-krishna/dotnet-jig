using FastEndpoints;
using Jig.Host;
using Jig.SharedKernel;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSingleton<IEventDispatcher, InProcessEventDispatcher>();

var moduleAssemblies = ModuleDiscovery.DiscoverAndRegister(builder.Services);

builder.Services.AddFastEndpoints(o =>
{
    o.DisableAutoDiscovery = true;
    o.Assemblies = moduleAssemblies;
});

var app = builder.Build();
app.UseFastEndpoints();
app.Run();
