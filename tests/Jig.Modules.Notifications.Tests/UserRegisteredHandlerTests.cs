using Jig.Modules.Notifications.Application;
using Jig.Modules.Notifications.Domain;
using Jig.Modules.Notifications.Infrastructure;
using Jig.Modules.Users.Contracts;
using Microsoft.Extensions.Options;
using Shouldly;
using Xunit;

namespace Jig.Modules.Notifications.Tests;

public class UserRegisteredHandlerTests
{
    private static UserRegisteredHandler NewHandler(INotificationStore store)
        => new(store, Options.Create(
            new NotificationsOptions { WelcomeMessageFormat = "Welcome, {0}!" }));

    [Fact]
    public async Task Handling_UserRegistered_records_a_welcome()
    {
        var store = new InMemoryNotificationStore();
        var handler = NewHandler(store);
        var userId = Guid.NewGuid();

        await handler.Handle(new UserRegistered(userId, "Ada", "ada@x.com"), TestContext.Current.CancellationToken);

        var notes = await store.ForUser(userId, TestContext.Current.CancellationToken);
        notes.ShouldHaveSingleItem().Message.ShouldContain("Ada");
    }
}
