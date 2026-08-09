using FastEndpoints;
using FastEndpoints.Swagger;
using Jig.Host;
using Jig.SharedKernel;
using Jig.Web;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseDefaultServiceProvider((_, options) =>
{
    options.ValidateScopes = true;
    options.ValidateOnBuild = true;
});

builder.Services.AddSingleton<IEventDispatcher, InProcessEventDispatcher>();

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
