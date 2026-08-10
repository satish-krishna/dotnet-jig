using System.Security.Claims;
using Jig.SharedKernel;

namespace Jig.Host.Security;

// The only place that knows the caller comes from HTTP. It maps the request principal's claims
// into ICurrentUser and exposes nothing else, so the coupling to HttpContext stops here in the host.
internal sealed class HttpContextCurrentUser(IHttpContextAccessor accessor) : ICurrentUser
{
    private ClaimsPrincipal? Principal => accessor.HttpContext?.User;

    public bool IsAuthenticated => Principal?.Identity?.IsAuthenticated ?? false;

    public PseudoKey? UserId =>
        Guid.TryParse(Principal?.FindFirst("sub")?.Value, out var id) ? new PseudoKey(id) : null;

    public IReadOnlySet<string> Scopes =>
        (Principal?.FindAll("scope").Select(c => c.Value) ?? []).ToHashSet();
}
