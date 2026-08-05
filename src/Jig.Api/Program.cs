using FastEndpoints;
using Jig.Application;
using Jig.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddApplication()
    .AddInfrastructure()
    .AddFastEndpoints();

var app = builder.Build();

app.UseFastEndpoints();
app.Run();
