using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Sprout.Application.Common.Exceptions;
using Sprout.Domain.Common;
using ValidationException = Sprout.Application.Common.Exceptions.ValidationException;

namespace Sprout.Api.Common;

/// <summary>
/// Turns the exceptions the Application and Domain layers throw into RFC 9457
/// problem documents. Everything the client needs to render a message inline is in
/// the body; nothing else leaks.
/// </summary>
public sealed class ApiExceptionHandler(
    IProblemDetailsService problemDetails,
    ILogger<ApiExceptionHandler> logger,
    IHostEnvironment environment) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext context,
        Exception exception,
        CancellationToken cancellationToken)
    {
        var (status, title, detail) = Map(exception);

        // Client mistakes are noise at Error level; server faults are not.
        if (status >= StatusCodes.Status500InternalServerError)
        {
            logger.LogError(exception, "Unhandled exception on {Method} {Path}", context.Request.Method, context.Request.Path);
        }
        else
        {
            logger.LogInformation("{Status} on {Method} {Path}: {Detail}", status, context.Request.Method, context.Request.Path, detail);
        }

        context.Response.StatusCode = status;

        var problem = new ProblemDetails
        {
            Status = status,
            Title = title,
            Detail = detail,
            Instance = $"{context.Request.Method} {context.Request.Path}",
        };

        // Field-level failures ride along under "errors", which is what the React
        // forms read to place messages next to inputs.
        if (exception is ValidationException validation)
        {
            problem.Extensions["errors"] = validation.Errors;
        }

        if (environment.IsDevelopment() && status >= StatusCodes.Status500InternalServerError)
        {
            problem.Extensions["exception"] = exception.ToString();
        }

        return await problemDetails.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = context,
            ProblemDetails = problem,
            Exception = exception,
        });
    }

    private static (int Status, string Title, string Detail) Map(Exception exception) => exception switch
    {
        ValidationException e => (StatusCodes.Status400BadRequest, "Check these fields", e.Message),
        DomainException e => (StatusCodes.Status400BadRequest, "That is not allowed", e.Message),
        UnauthorisedException e => (StatusCodes.Status401Unauthorized, "Not signed in", e.Message),
        ForbiddenException e => (StatusCodes.Status403Forbidden, "Not allowed", e.Message),
        NotFoundException e => (StatusCodes.Status404NotFound, "Not found", e.Message),
        ConflictException e => (StatusCodes.Status409Conflict, "That clashes with something", e.Message),
        // 499 is nginx's code for a client that hung up; the framework has no constant for it.
        OperationCanceledException => (499, "Cancelled", "The request was cancelled."),
        _ => (StatusCodes.Status500InternalServerError, "Something went wrong", "Sprout could not complete that. Try again."),
    };
}
