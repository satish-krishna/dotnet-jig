using Microsoft.Extensions.DependencyInjection;

namespace Jig.Infrastructure;

/// <summary>The infrastructure layer owns its own edges: persistence, external clients,
/// the things that hold a connection string. It is empty until the Persistence decision
/// fills it on its own branch. The composition root only names the module.</summary>
public static class InfrastructureModule
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services)
    {
        return services;
    }
}
