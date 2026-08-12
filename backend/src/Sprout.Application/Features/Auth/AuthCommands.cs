using FluentValidation;
using MediatR;
using Sprout.Application.Common.Abstractions;
using Sprout.Application.Common.Contracts;
using Sprout.Application.Common.Exceptions;

namespace Sprout.Application.Features.Auth;

// ── Requests ───────────────────────────────────────────────────────────────────

/// <summary>Creates an email/password account and signs it in, as "Create account" does.</summary>
public sealed record RegisterCommand(string Email, string Password, string? DisplayName)
    : IRequest<AuthResultDto>;

/// <summary>Signs in an existing email/password account.</summary>
public sealed record LoginCommand(string Email, string Password) : IRequest<AuthResultDto>;

/// <summary>
/// Exchanges a Google ID token, obtained by the client from Google Identity Services,
/// for a Sprout session. Creates the account on first sign-in.
/// </summary>
public sealed record GoogleLoginCommand(string IdToken) : IRequest<AuthResultDto>;

/// <summary>Trades a refresh token for a fresh access token.</summary>
public sealed record RefreshTokenCommand(string RefreshToken) : IRequest<AuthResultDto>;

/// <summary>Revokes a refresh token. The client drops both tokens either way.</summary>
public sealed record LogoutCommand(string RefreshToken) : IRequest<Unit>;

/// <summary>The signed-in account, for restoring a session on page load.</summary>
public sealed record GetCurrentUserQuery : IRequest<UserDto>;

// ── Validators ─────────────────────────────────────────────────────────────────

public sealed class RegisterCommandValidator : AbstractValidator<RegisterCommand>
{
    public RegisterCommandValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Enter your email address.")
            .EmailAddress().WithMessage("That does not look like an email address.")
            .MaximumLength(256);

        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("Choose a password.")
            .MinimumLength(8).WithMessage("Use at least 8 characters.")
            .MaximumLength(128);

        RuleFor(x => x.DisplayName).MaximumLength(80);
    }
}

public sealed class LoginCommandValidator : AbstractValidator<LoginCommand>
{
    public LoginCommandValidator()
    {
        RuleFor(x => x.Email).NotEmpty().WithMessage("Enter your email address.");
        RuleFor(x => x.Password).NotEmpty().WithMessage("Enter your password.");
    }
}

public sealed class GoogleLoginCommandValidator : AbstractValidator<GoogleLoginCommand>
{
    public GoogleLoginCommandValidator() =>
        RuleFor(x => x.IdToken).NotEmpty().WithMessage("Google did not return a token.");
}

public sealed class RefreshTokenCommandValidator : AbstractValidator<RefreshTokenCommand>
{
    public RefreshTokenCommandValidator() => RuleFor(x => x.RefreshToken).NotEmpty();
}

// ── Handlers ───────────────────────────────────────────────────────────────────

public sealed class AuthHandlers(IIdentityService identity, ICurrentUser currentUser) :
    IRequestHandler<RegisterCommand, AuthResultDto>,
    IRequestHandler<LoginCommand, AuthResultDto>,
    IRequestHandler<GoogleLoginCommand, AuthResultDto>,
    IRequestHandler<RefreshTokenCommand, AuthResultDto>,
    IRequestHandler<LogoutCommand, Unit>,
    IRequestHandler<GetCurrentUserQuery, UserDto>
{
    public async Task<AuthResultDto> Handle(RegisterCommand request, CancellationToken ct) =>
        ToDto(await identity.RegisterAsync(request.Email, request.Password, request.DisplayName, ct));

    public async Task<AuthResultDto> Handle(LoginCommand request, CancellationToken ct) =>
        ToDto(await identity.SignInAsync(request.Email, request.Password, ct));

    public async Task<AuthResultDto> Handle(GoogleLoginCommand request, CancellationToken ct) =>
        ToDto(await identity.SignInWithGoogleAsync(request.IdToken, ct));

    public async Task<AuthResultDto> Handle(RefreshTokenCommand request, CancellationToken ct) =>
        ToDto(await identity.RefreshAsync(request.RefreshToken, ct));

    public async Task<Unit> Handle(LogoutCommand request, CancellationToken ct)
    {
        await identity.RevokeRefreshTokenAsync(request.RefreshToken, ct);
        return Unit.Value;
    }

    public async Task<UserDto> Handle(GetCurrentUserQuery request, CancellationToken ct)
    {
        var userId = currentUser.RequireUserId();
        var profile = await identity.FindByIdAsync(userId, ct)
            ?? throw new UnauthorisedException("That session no longer matches an account.");

        return ToDto(profile);
    }

    internal static UserDto ToDto(UserProfile p) =>
        new(p.Id, p.Email, p.DisplayName, p.Initials, p.AvatarColor);

    private static AuthResultDto ToDto(AuthenticatedUser result) =>
        new(ToDto(result.User), result.AccessToken, result.AccessTokenExpiresAt, result.RefreshToken);
}
