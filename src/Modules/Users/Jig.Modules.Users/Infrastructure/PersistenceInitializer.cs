using Jig.Modules.Users.Infrastructure.EfCore;
using Jig.Modules.Users.Infrastructure.Mongo;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using MongoDB.Driver;

namespace Jig.Modules.Users.Infrastructure;

/// <summary>Prepares whichever store the configured provider selected, once, at host startup:
/// EF gets its schema, Mongo gets its unique index. Living here as a hosted service is what
/// keeps the host provider-agnostic; it starts every module's hosted services without needing
/// to know what persistence backs any of them.
///
/// Registered as a singleton (as every <see cref="IHostedService"/> is), it resolves its own
/// scope at <see cref="StartAsync"/> rather than taking the scoped store as a constructor
/// dependency, so it never becomes a captive dependency itself.</summary>
internal sealed class PersistenceInitializer(IServiceProvider services, IOptions<PersistenceOptions> options) : IHostedService
{
    public async Task StartAsync(CancellationToken ct)
    {
        using var scope = services.CreateScope();

        if (options.Value.Provider == "Mongo")
        {
            var collection = scope.ServiceProvider.GetRequiredService<IMongoCollection<UserDocument>>();
            await MongoUserStore.EnsureIndexes(collection, ct);
        }
        else
        {
            var db = scope.ServiceProvider.GetRequiredService<JigDbContext>();
            await db.Database.EnsureCreatedAsync(ct);
        }
    }

    public Task StopAsync(CancellationToken ct) => Task.CompletedTask;
}
