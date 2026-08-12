namespace Sprout.Application.Common.Exceptions;

/// <summary>The caller is not signed in. Mapped to 401.</summary>
public class UnauthorisedException(string message = "You need to be signed in.") : Exception(message);

/// <summary>The caller is signed in but not allowed to do this. Mapped to 403.</summary>
public class ForbiddenException(string message = "You do not have access to that.") : Exception(message);

/// <summary>The addressed resource does not exist, or is not visible to the caller. Mapped to 404.</summary>
public class NotFoundException(string what) : Exception($"{what} could not be found.");

/// <summary>Request-shape or business-rule validation failed. Mapped to 400 with a field map.</summary>
public class ValidationException(IReadOnlyDictionary<string, string[]> errors)
    : Exception("One or more fields need attention.")
{
    public IReadOnlyDictionary<string, string[]> Errors { get; } = errors;

    public static ValidationException ForField(string field, string message) =>
        new(new Dictionary<string, string[]> { [field] = [message] });
}

/// <summary>The request conflicts with current state, e.g. a duplicate name. Mapped to 409.</summary>
public class ConflictException(string message) : Exception(message);
