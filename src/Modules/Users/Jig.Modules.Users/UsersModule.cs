using Jig.Modules.Users.Application;
using Jig.Modules.Users.Contracts;
using Jig.Modules.Users.Domain;
using Jig.Modules.Users.Infrastructure;
using Jig.SharedKernel;
using Microsoft.Extensions.DependencyInjection;

namespace Jig.Modules.Users;

internal sealed class UsersModule : IModule
{
    public void Register(IServiceCollection services)
    {
        // Singleton is correct only while the store is in memory. When persistence
        // moves to a scoped store on the persistence branch, both of these flip to
        // scoped (Risk R2, the captive-dependency trap).
        services.AddSingleton<IUserStore, InMemoryUserStore>();
        services.AddSingleton<UserService>();
        services.AddSingleton<IUserDirectory, UserDirectory>();
    }
}
