using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace Jig.Web;

/// <summary>The terminal handler in the exception chain. Anything that reaches here is genuinely
/// unexpected: it is logged with a correlation id and returned to the caller as one stable
/// ProblemDetails shape. Nothing internal crosses the boundary.</summary>
public sealed class FallbackExceptionHandler : IExceptionHandler
{
    private readonly ILogger<FallbackExceptionHandler> _log;

    public FallbackExceptionHandler(ILogger<FallbackExceptionHandler> log) => _log = log;

    public async ValueTask<bool> TryHandleAsync(HttpContext ctx, Exception ex, CancellationToken ct)
    {
        var correlationId = ctx.TraceIdentifier;
        _log.LogError(ex, "Unhandled exception {CorrelationId}", correlationId);

        await Results.Problem(
            title: "An unexpected error occurred.",
            statusCode: StatusCodes.Status500InternalServerError,
            extensions: new Dictionary<string, object?> { ["correlationId"] = correlationId }
        ).ExecuteAsync(ctx);

        return true;
    }
}
