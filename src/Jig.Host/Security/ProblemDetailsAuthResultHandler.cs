using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Policy;

namespace Jig.Host.Security;

// Turns an authorization failure (403) into ProblemDetails, so it matches every other error in
// the API since Part 3. The 401 challenge is handled by JwtBearerEvents.OnChallenge in Program.cs;
// this covers the authenticated-but-forbidden case.
internal sealed class ProblemDetailsAuthResultHandler(IProblemDetailsService problems)
    : IAuthorizationMiddlewareResultHandler
{
    private readonly AuthorizationMiddlewareResultHandler _default = new();

    public async Task HandleAsync(RequestDelegate next, HttpContext context,
        AuthorizationPolicy policy, PolicyAuthorizationResult authorizeResult)
    {
        if (authorizeResult.Forbidden)
        {
            context.Response.StatusCode = StatusCodes.Status403Forbidden;
            await problems.WriteAsync(new ProblemDetailsContext
            {
                HttpContext = context,
                ProblemDetails = { Status = StatusCodes.Status403Forbidden, Title = "Forbidden" },
            });
            return;
        }

        await _default.HandleAsync(next, context, policy, authorizeResult);
    }
}
