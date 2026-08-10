using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace Jig.Host.Tests.Security;

// Mints tokens the test host will accept. Must match the Security config JigApiFactory sets.
internal static class DevTokens
{
    public const string Issuer = "jig-tests";
    public const string Audience = "jig";
    public const string Key = "dev-signing-key-at-least-32-bytes-long!!";

    public static string Person(string subject, params string[] scopes)
    {
        var claims = new List<Claim> { new("sub", subject) };
        claims.AddRange(scopes.Select(s => new Claim("scope", s)));
        var descriptor = new SecurityTokenDescriptor
        {
            Issuer = Issuer,
            Audience = Audience,
            Subject = new ClaimsIdentity(claims),
            SigningCredentials = new SigningCredentials(
                new SymmetricSecurityKey(Encoding.UTF8.GetBytes(Key)), SecurityAlgorithms.HmacSha256),
        };
        return new JsonWebTokenHandler().CreateToken(descriptor);
    }
}
