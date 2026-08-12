namespace Sprout.Application.Common.Abstractions;

/// <summary>
/// The authenticated caller, resolved from the bearer token by Infrastructure.
/// Handlers depend on this rather than on HttpContext, so swapping the token issuer
/// (self-issued JWT today, a hosted provider later) does not reach the Application layer.
/// </summary>
public interface ICurrentUser
{
    /// <summary>Null when the request is anonymous.</summary>
    Guid? UserId { get; }

    string? Email { get; }

    bool IsAuthenticated => UserId is not null;

    /// <summary>The caller's id, or a 401 if the request is anonymous.</summary>
    Guid RequireUserId() => UserId ?? throw new Sprout.Application.Common.Exceptions.UnauthorisedException();
}
