using FluentValidation;
using MediatR;
using ValidationException = Sprout.Application.Common.Exceptions.ValidationException;

namespace Sprout.Application.Common.Behaviours;

/// <summary>
/// Runs every FluentValidation validator registered for a request before its handler.
/// Failures surface as one <see cref="ValidationException"/> carrying a field map,
/// which the API renders as an RFC 9457 problem document.
/// </summary>
public sealed class ValidationBehaviour<TRequest, TResponse>(IEnumerable<IValidator<TRequest>> validators)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        if (!validators.Any())
        {
            return await next(cancellationToken);
        }

        var context = new ValidationContext<TRequest>(request);
        var results = await Task.WhenAll(validators.Select(v => v.ValidateAsync(context, cancellationToken)));

        var failures = results
            .SelectMany(r => r.Errors)
            .Where(f => f is not null)
            .GroupBy(f => Camelise(f.PropertyName))
            .ToDictionary(g => g.Key, g => g.Select(f => f.ErrorMessage).Distinct().ToArray());

        return failures.Count > 0
            ? throw new ValidationException(failures)
            : await next(cancellationToken);
    }

    // Field names go over the wire in the same casing the React forms use.
    private static string Camelise(string property) =>
        string.IsNullOrEmpty(property) || char.IsLower(property[0])
            ? property
            : char.ToLowerInvariant(property[0]) + property[1..];
}
