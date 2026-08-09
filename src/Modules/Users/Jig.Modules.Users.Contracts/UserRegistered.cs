using Jig.SharedKernel;

namespace Jig.Modules.Users.Contracts;

public record UserRegistered(Guid UserId, string Name, string Email) : IIntegrationEvent;
