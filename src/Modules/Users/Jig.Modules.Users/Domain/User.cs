using Jig.SharedKernel;

namespace Jig.Modules.Users.Domain;

internal record User(PseudoKey Id, string Name, string Email);
