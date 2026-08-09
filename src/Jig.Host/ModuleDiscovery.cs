using System.Reflection;
using Jig.SharedKernel;
using Microsoft.Extensions.DependencyInjection;

namespace Jig.Host;

internal static class ModuleDiscovery
{
    public static Assembly[] DiscoverAndRegister(IServiceCollection services)
    {
        var moduleAssemblies = new[]
        {
            typeof(Jig.Modules.Users.UsersModuleMarker).Assembly,
        };

        foreach (var assembly in moduleAssemblies)
        {
            var moduleTypes = assembly.GetTypes()
                .Where(t => typeof(IModule).IsAssignableFrom(t) && t is { IsAbstract: false, IsInterface: false });

            foreach (var moduleType in moduleTypes)
            {
                var module = (IModule)Activator.CreateInstance(moduleType)!;
                module.Register(services);
            }
        }

        return moduleAssemblies.Distinct().ToArray();
    }
}
