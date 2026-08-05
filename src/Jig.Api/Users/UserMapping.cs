using Jig.Domain;

namespace Jig.Api.Users;

/// <summary>Maps the domain User to the wire response. One place, so the mapping
/// cannot drift endpoint to endpoint.</summary>
public static class UserMapping
{
    public static UserResponse ToResponse(this User user) => new()
    {
        Id = user.Id,
        Name = user.Name,
        Email = user.Email,
    };
}
