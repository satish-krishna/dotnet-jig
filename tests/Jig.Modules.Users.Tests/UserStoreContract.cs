using Jig.Modules.Users.Domain;
using Jig.SharedKernel;
using Shouldly;

namespace Jig.Modules.Users.Tests;

internal static class UserStoreContract
{
    public static async Task Run(IUserStore store, CancellationToken ct)
    {
        var ada = new User(PseudoKey.New(), "Ada", "Ada@Example.com");
        await store.Add(ada, ct);

        (await store.Find(ada.Id, ct))!.Email.ShouldBe("Ada@Example.com");
        (await store.FindByEmail("ada@example.com", ct)).ShouldNotBeNull();   // case-insensitive
        (await store.All(ct)).ShouldContain(u => u.Id == ada.Id);
        (await store.Find(PseudoKey.New(), ct)).ShouldBeNull();
    }
}
