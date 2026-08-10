namespace Jig.SharedKernel;

// The caller, as a module sees it: a small shape, deliberately not the ClaimsPrincipal, so HTTP
// and claims never leak into a module. The host projects the authenticated principal into this;
// a module injects it and never learns the identity came from HTTP. This is what lets identity
// cross into a module without the module depending on the web host.
public interface ICurrentUser
{
    bool IsAuthenticated { get; }
    PseudoKey? UserId { get; }
    IReadOnlySet<string> Scopes { get; }
}
