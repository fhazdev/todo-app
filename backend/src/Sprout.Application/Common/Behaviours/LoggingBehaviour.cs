using System.Diagnostics;
using MediatR;
using Microsoft.Extensions.Logging;
using Sprout.Application.Common.Abstractions;

namespace Sprout.Application.Common.Behaviours;

/// <summary>
/// One log line per request with its duration and the acting user, and a warning
/// for anything slower than half a second.
/// </summary>
public sealed class LoggingBehaviour<TRequest, TResponse>(
    ILogger<LoggingBehaviour<TRequest, TResponse>> logger,
    ICurrentUser currentUser)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    private const int SlowRequestMilliseconds = 500;

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        var name = typeof(TRequest).Name;
        var stopwatch = Stopwatch.StartNew();

        var response = await next(cancellationToken);

        stopwatch.Stop();
        var elapsed = stopwatch.ElapsedMilliseconds;

        if (elapsed > SlowRequestMilliseconds)
        {
            logger.LogWarning(
                "Sprout request {RequestName} took {Elapsed}ms for user {UserId}",
                name, elapsed, currentUser.UserId);
        }
        else
        {
            logger.LogInformation(
                "Sprout request {RequestName} handled in {Elapsed}ms for user {UserId}",
                name, elapsed, currentUser.UserId);
        }

        return response;
    }
}
