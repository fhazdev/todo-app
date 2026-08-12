using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Sprout.Api.Contracts;
using Sprout.Application.Common.Contracts;
using Sprout.Application.Features.Auth;

namespace Sprout.Api.Controllers;

/// <summary>
/// Sign in, sign up, refresh and sign out. Anonymous access is opened per action
/// rather than on the class, so /auth/me keeps the base controller's [Authorize].
/// </summary>
public sealed class AuthController(ISender mediator) : ApiControllerBase(mediator)
{
    /// <summary>Creates an email/password account and returns a session.</summary>
    [AllowAnonymous]
    [HttpPost("register")]
    [ProducesResponseType<AuthResultDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<AuthResultDto>> Register(RegisterRequest request, CancellationToken ct) =>
        Ok(await Mediator.Send(new RegisterCommand(request.Email, request.Password, request.DisplayName), ct));

    /// <summary>Signs in an existing email/password account.</summary>
    [AllowAnonymous]
    [HttpPost("login")]
    [ProducesResponseType<AuthResultDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<AuthResultDto>> Login(LoginRequest request, CancellationToken ct) =>
        Ok(await Mediator.Send(new LoginCommand(request.Email, request.Password), ct));

    /// <summary>
    /// Exchanges a Google ID token for a Sprout session, creating the account on
    /// first sign-in.
    /// </summary>
    [AllowAnonymous]
    [HttpPost("google")]
    [ProducesResponseType<AuthResultDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<AuthResultDto>> Google(GoogleLoginRequest request, CancellationToken ct) =>
        Ok(await Mediator.Send(new GoogleLoginCommand(request.IdToken), ct));

    /// <summary>Rotates a refresh token and returns a fresh access token.</summary>
    [AllowAnonymous]
    [HttpPost("refresh")]
    [ProducesResponseType<AuthResultDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<AuthResultDto>> Refresh(RefreshRequest request, CancellationToken ct) =>
        Ok(await Mediator.Send(new RefreshTokenCommand(request.RefreshToken), ct));

    /// <summary>Revokes a refresh token. Always succeeds, so the client can clear state either way.</summary>
    [AllowAnonymous]
    [HttpPost("logout")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Logout(RefreshRequest request, CancellationToken ct)
    {
        await Mediator.Send(new LogoutCommand(request.RefreshToken), ct);
        return NoContent();
    }

    /// <summary>The signed-in account, for restoring a session on page load.</summary>
    [HttpGet("me")]
    [ProducesResponseType<UserDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<UserDto>> Me(CancellationToken ct) =>
        Ok(await Mediator.Send(new GetCurrentUserQuery(), ct));
}
