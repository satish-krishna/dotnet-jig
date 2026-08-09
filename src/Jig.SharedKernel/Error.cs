namespace Jig.SharedKernel;

public enum ErrorKind { Validation, NotFound, Conflict, Unexpected }

public sealed record Error(string Code, string Message, ErrorKind Kind)
{
    public static Error NotFound(string message) => new("not_found", message, ErrorKind.NotFound);
    public static Error Conflict(string message) => new("conflict", message, ErrorKind.Conflict);
    public static Error Validation(string message) => new("validation", message, ErrorKind.Validation);
}
