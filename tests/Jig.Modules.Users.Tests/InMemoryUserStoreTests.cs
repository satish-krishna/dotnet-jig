using Jig.Modules.Users.Infrastructure;
using Xunit;

namespace Jig.Modules.Users.Tests;

public class InMemoryUserStoreTests
{
    [Fact]
    public async Task Honors_the_store_contract()
        => await UserStoreContract.Run(new InMemoryUserStore(), TestContext.Current.CancellationToken);
}
