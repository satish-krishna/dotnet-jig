using Jig.Modules.Notifications.Application;
using Jig.Modules.Notifications.Infrastructure;
using Jig.Modules.Users.Contracts;
using Shouldly;
using Xunit;

namespace Jig.Modules.Notifications.Tests;

public class UserRegisteredHandlerTests
{
    [Fact]
    public async Task Handling_UserRegistered_records_a_welcome()
    {
        var store = new InMemoryNotificationStore();
        var handler = new UserRegisteredHandler(store);
        var userId = Guid.NewGuid();

        await handler.Handle(new UserRegistered(userId, "Ada", "ada@x.com"), TestContext.Current.CancellationToken);

        var notes = await store.ForUser(userId, TestContext.Current.CancellationToken);
        notes.ShouldHaveSingleItem().Message.ShouldContain("Ada");
    }
}
