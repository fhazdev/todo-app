namespace Sprout.Domain.Common;

/// <summary>
/// Raised when an operation would break an invariant the domain guarantees.
/// The API maps this to 400; it is never a server fault.
/// </summary>
public class DomainException(string message) : Exception(message);
