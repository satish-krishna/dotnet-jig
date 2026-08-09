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

        // A case-different duplicate email is a modeled Conflict from the store, not a thrown
        // exception the caller has to translate. The store is the single arbiter of uniqueness.
        var duplicate = new User(PseudoKey.New(), "Ada Two", "ada@example.com");
        var addDuplicate = await store.Add(duplicate, ct);
        addDuplicate.IsSuccess.ShouldBeFalse();
        addDuplicate.Error!.Kind.ShouldBe(ErrorKind.Conflict);
    }
}
