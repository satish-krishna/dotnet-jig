using System.ComponentModel.DataAnnotations;

namespace Jig.Host.Security;

// Validate-only auth config. In production Authority points at a real IdP and the handler
// validates against its published keys; for the template and its tests, DevSigningKey is a
// symmetric key the dev-token endpoint and tests sign with. ApiKeys are machine callers,
// from config only: a real key store, hashing, and rotation are described in the post, not built.
internal sealed class SecurityOptions
{
    [Required] public string Issuer { get; set; } = "";
    [Required] public string Audience { get; set; } = "";
    public string? Authority { get; set; }
    [Required, MinLength(32)] public string DevSigningKey { get; set; } = "";
    public Dictionary<string, ApiKeyOptions> ApiKeys { get; set; } = new();
}

internal sealed class ApiKeyOptions
{
    [Required] public string Subject { get; set; } = "";
    public string[] Scopes { get; set; } = [];
}
