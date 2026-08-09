using FastEndpoints;
using FluentValidation;

namespace Jig.Modules.Users.Transport;

/// <summary>Shape validation for POST /users. Runs before the handler; a failure returns 400
/// with the field errors. Business rules (a duplicate email) live in UserService and come back
/// as a modeled Conflict, not from here.</summary>
internal sealed class CreateUserValidator : Validator<CreateUserRequest>
{
    public CreateUserValidator()
    {
        RuleFor(x => x.Name).NotEmpty();
        RuleFor(x => x.Email).NotEmpty().EmailAddress();
    }
}
