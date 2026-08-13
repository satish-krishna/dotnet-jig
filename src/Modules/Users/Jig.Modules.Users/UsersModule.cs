using Jig.Modules.Users.Application;
using Jig.Modules.Users.Contracts;
using Jig.Modules.Users.Domain;
using Jig.Modules.Users.Infrastructure;
using Jig.Modules.Users.Infrastructure.EfCore;
using Jig.Modules.Users.Infrastructure.Mongo;
using Jig.SharedKernel;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using MongoDB.Driver;

namespace Jig.Modules.Users;

internal sealed class UsersModule : IModule
{
    public void Register(IServiceCollection services)
    {
        services.AddOptions<PersistenceOptions>()
            .BindConfiguration("Persistence")
            .ValidateDataAnnotations()
            .ValidateOnStart();

        // EF Core side. Registered unconditionally: AddDbContext/AddScoped only describe how to
        // build the type, they do not build it, so this costs nothing when Mongo is configured.
        services.AddDbContext<JigDbContext>((sp, o) =>
            o.UseSqlite(sp.GetRequiredService<IOptions<PersistenceOptions>>().Value.ConnectionString));
        services.AddScoped<EfUserStore>();

        // Mongo side. The client and collection are thread-safe, long-lived handles (the driver's
        // own guidance), so they are singletons; MongoUserStore itself stays scoped for parity
        // with EfUserStore.
        services.AddSingleton<IMongoClient>(sp =>
            new MongoClient(sp.GetRequiredService<IOptions<PersistenceOptions>>().Value.ConnectionString));
        services.AddSingleton(sp =>
        {
            var opts = sp.GetRequiredService<IOptions<PersistenceOptions>>().Value;
            return sp.GetRequiredService<IMongoClient>()
                .GetDatabase(opts.DatabaseName)
                .GetCollection<UserDocument>("users");
        });
        services.AddScoped<MongoUserStore>();

        // The seam: which concrete store backs IUserStore is a runtime decision read from
        // options, not a compile-time or registration-time one, so it is resolved inside the
        // factory rather than branched on while building the container.
        services.AddScoped<IUserStore>(sp =>
        {
            var opts = sp.GetRequiredService<IOptions<PersistenceOptions>>().Value;
            return opts.Provider == "Mongo"
                ? sp.GetRequiredService<MongoUserStore>()
                : sp.GetRequiredService<EfUserStore>();
        });

        // Scoped, not singleton: both depend on the now-scoped IUserStore, and a real store
        // (EF's DbContext, Mongo's session-bound handles) cannot be captured by a singleton
        // without either becoming a captive dependency or leaking across requests. ValidateScopes
        // and ValidateOnBuild on the host container catch this class of mistake at boot.
        services.AddScoped<UserService>();
        services.AddScoped<IUserDirectory, UserDirectory>();

        // Schema/index preparation runs once at host startup regardless of provider, so the host
        // itself never needs to know which one is configured.
        services.AddHostedService<PersistenceInitializer>();
    }
}
