using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;

namespace Jig.Host.Security;

// Machine callers authenticate with an API key, resolved from config to a subject and scopes. It
// emits the same claim shape as the bearer scheme (sub + scope claims), so authorization downstream
// does not care which scheme authenticated the caller: one policy set serves person and machine.
internal sealed class ApiKeyAuthHandler(
    IOptionsMonitor<AuthenticationSchemeOptions> options,
    ILoggerFactory logger,
    UrlEncoder encoder,
    IOptions<SecurityOptions> security)
    : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
{
    public const string SchemeName = "ApiKey";
    public const string HeaderName = "X-Api-Key";

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (!Request.Headers.TryGetValue(HeaderName, out var header) || header.Count == 0)
            return Task.FromResult(AuthenticateResult.NoResult());

        if (!security.Value.ApiKeys.TryGetValue(header.ToString(), out var key))
            return Task.FromResult(AuthenticateResult.Fail("Unknown API key"));

        var claims = new List<Claim> { new("sub", key.Subject) };
        claims.AddRange(key.Scopes.Select(s => new Claim("scope", s)));
        var principal = new ClaimsPrincipal(new ClaimsIdentity(claims, SchemeName));
        return Task.FromResult(AuthenticateResult.Success(new AuthenticationTicket(principal, SchemeName)));
    }

    // Keep the API-key 401 in the same ProblemDetails contract as the bearer challenge.
    protected override async Task HandleChallengeAsync(AuthenticationProperties properties)
    {
        Response.StatusCode = StatusCodes.Status401Unauthorized;
        var problems = Context.RequestServices.GetRequiredService<IProblemDetailsService>();
        await problems.WriteAsync(new ProblemDetailsContext
        {
            HttpContext = Context,
            ProblemDetails = { Status = StatusCodes.Status401Unauthorized, Title = "Unauthorized" },
        });
    }
}
