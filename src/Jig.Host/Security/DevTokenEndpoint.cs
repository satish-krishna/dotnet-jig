using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace Jig.Host.Security;

internal sealed record DevTokenRequest(string Subject, string[]? Scopes);

// Development-only. Mints a token signed with the dev key so the rig and manual testing can call
// protected endpoints. Never mapped outside Development; in production tokens come from a real IdP.
internal static class DevTokenEndpoint
{
    public static void MapDevTokenEndpoint(this WebApplication app)
    {
        if (!app.Environment.IsDevelopment()) return;

        app.MapPost("/dev/token", (DevTokenRequest req, IOptions<SecurityOptions> sec) =>
                Results.Ok(new { token = Mint(sec.Value, req.Subject, req.Scopes ?? []) }))
           .AllowAnonymous();
    }

    private static string Mint(SecurityOptions security, string subject, IEnumerable<string> scopes)
    {
        var claims = new List<Claim> { new("sub", subject) };
        claims.AddRange(scopes.Select(s => new Claim("scope", s)));
        var descriptor = new SecurityTokenDescriptor
        {
            Issuer = security.Issuer,
            Audience = security.Audience,
            Subject = new ClaimsIdentity(claims),
            SigningCredentials = new SigningCredentials(
                new SymmetricSecurityKey(Encoding.UTF8.GetBytes(security.DevSigningKey)), SecurityAlgorithms.HmacSha256),
        };
        return new JsonWebTokenHandler().CreateToken(descriptor);
    }
}
