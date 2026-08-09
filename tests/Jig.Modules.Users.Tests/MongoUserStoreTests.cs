using EphemeralMongo;
using Jig.Modules.Users.Infrastructure.Mongo;
using MongoDB.Driver;
using Xunit;

namespace Jig.Modules.Users.Tests;

public class MongoUserStoreTests
{
    [Fact]
    public async Task Honors_the_store_contract()
    {
#pragma warning disable xUnit1051 // MongoRunner.Run() has no CancellationToken overload
        using var runner = MongoRunner.Run();
#pragma warning restore xUnit1051
        var col = new MongoClient(runner.ConnectionString)
            .GetDatabase("jig_test")
            .GetCollection<UserDocument>("users");
        await MongoUserStore.EnsureIndexes(col, TestContext.Current.CancellationToken);

        await UserStoreContract.Run(new MongoUserStore(col), TestContext.Current.CancellationToken);
    }
}
