using System.Reflection;
using Jig.SharedKernel;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyModel;

namespace Jig.Host;

internal static class ModuleDiscovery
{
    public static Assembly[] DiscoverAndRegister(IServiceCollection services)
    {
        var moduleAssemblies = DependencyContext.Default!.RuntimeLibraries
            .Where(l => l.Name.StartsWith("Jig.Modules.", StringComparison.Ordinal)
                        && !l.Name.EndsWith(".Contracts", StringComparison.Ordinal))
            .Select(l => Assembly.Load(new AssemblyName(l.Name)))
            .Distinct()
            .ToArray();

        foreach (var assembly in moduleAssemblies)
            foreach (var moduleType in assembly.GetTypes()
                         .Where(t => typeof(IModule).IsAssignableFrom(t) && t is { IsAbstract: false, IsInterface: false }))
                ((IModule)Activator.CreateInstance(moduleType)!).Register(services);

        return moduleAssemblies;
    }
}
