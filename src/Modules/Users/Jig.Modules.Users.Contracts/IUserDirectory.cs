using Jig.SharedKernel;

namespace Jig.Modules.Users.Contracts;

public interface IUserDirectory
{
    Task<Result<UserSummary>> GetById(Guid id, CancellationToken ct);
}
