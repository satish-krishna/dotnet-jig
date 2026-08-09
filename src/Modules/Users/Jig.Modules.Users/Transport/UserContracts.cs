using Jig.Modules.Users.Domain;

namespace Jig.Modules.Users.Transport;

internal sealed record CreateUserRequest(string Name, string Email);
internal sealed record GetUserRequest(Guid Id);
internal sealed record UserResponse(string Id, string Name, string Email);

internal static class UserMapping
{
    public static UserResponse ToResponse(this User user)
        => new(user.Id.ToString(), user.Name, user.Email);
}
