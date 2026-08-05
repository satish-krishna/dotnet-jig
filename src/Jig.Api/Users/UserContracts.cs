namespace Jig.Api.Users;

/// <summary>The user shape on the wire. Separate from the domain User on purpose:
/// the transport type and the domain type are allowed to move independently.</summary>
public sealed class UserResponse
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string Email { get; set; } = "";
}

/// <summary>Route request for GET /users/{id}.</summary>
public sealed class GetUserRequest
{
    public int Id { get; set; }
}

/// <summary>Body request for POST /users.</summary>
public sealed class CreateUserRequest
{
    public string Name { get; set; } = "";
    public string Email { get; set; } = "";
}
