using Microsoft.Extensions.DependencyInjection;

namespace Jig.SharedKernel;

/// <summary>A module registers its own internals. The host discovers modules by
/// reflection and calls Register on each, so it never learns what is inside them.</summary>
public interface IModule
{
    void Register(IServiceCollection services);
}
