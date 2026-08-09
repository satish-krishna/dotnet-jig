using FastEndpoints;
using Jig.Host;

var builder = WebApplication.CreateBuilder(args);

var moduleAssemblies = ModuleDiscovery.DiscoverAndRegister(builder.Services);

builder.Services.AddFastEndpoints(o =>
{
    o.DisableAutoDiscovery = true;
    o.Assemblies = moduleAssemblies;
});

var app = builder.Build();
app.UseFastEndpoints();
app.Run();
