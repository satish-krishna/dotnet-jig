using Microsoft.Extensions.DependencyInjection;

namespace Jig.Application;

/// <summary>The application layer registers its own use-cases, so the composition
/// root names the module and never learns what is inside it.</summary>
public static class ApplicationModule
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        // Singleton because the store lives in the service for now. When persistence
        // moves out to Infrastructure, this goes back to scoped.
        services.AddSingleton<UserService>();
        return services;
    }
}
