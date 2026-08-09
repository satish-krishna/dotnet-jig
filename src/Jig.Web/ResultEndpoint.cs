using FastEndpoints;
using Jig.SharedKernel;

namespace Jig.Web;

/// <summary>Base endpoint that turns a Result envelope into a transport response: success sends
/// the mapped body, failure maps the error kind to an HTTP status. One place, so no endpoint
/// invents its own status handling.</summary>
public abstract class ResultEndpoint<TRequest, TResponse> : Endpoint<TRequest, TResponse>
    where TRequest : notnull
{
    protected async Task SendResultAsync<T>(Result<T> result, Func<T, TResponse> map, CancellationToken ct)
    {
        if (result.IsSuccess)
        {
            await Send.OkAsync(map(result.Value!), ct);
            return;
        }

        var status = result.Error!.Kind switch
        {
            ErrorKind.NotFound => 404,
            ErrorKind.Validation => 400,
            ErrorKind.Conflict => 409,
            _ => 500,
        };
        AddError(result.Error.Message);
        await Send.ErrorsAsync(status, ct);
    }
}
