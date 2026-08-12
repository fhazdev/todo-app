using FluentValidation;
using MediatR;
using Sprout.Application.Common.Abstractions;
using Sprout.Application.Common.Contracts;
using Sprout.Application.Common.Exceptions;
using Sprout.Application.Common.Services;
using Sprout.Domain.Lists;

namespace Sprout.Application.Features.Members;

// ── Requests ───────────────────────────────────────────────────────────────────

/// <summary>The Shared with screen: owner, editors, then pending invitations.</summary>
public sealed record GetMembersQuery(Guid ListId) : IRequest<IReadOnlyList<MemberDto>>;

/// <summary>
/// Invites someone by email. If that address already has an account the membership
/// goes straight to active; otherwise it stays pending until they sign up.
/// </summary>
public sealed record InviteMemberCommand(Guid ListId, string Email) : IRequest<MemberDto>;

/// <summary>Removes a member or withdraws an invitation. Owner only, and never the owner.</summary>
public sealed record RemoveMemberCommand(Guid ListId, Guid MemberId) : IRequest<Unit>;

// ── Validators ─────────────────────────────────────────────────────────────────

public sealed class InviteMemberCommandValidator : AbstractValidator<InviteMemberCommand>
{
    public InviteMemberCommandValidator() =>
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Enter an email address.")
            .EmailAddress().WithMessage("That does not look like an email address.")
            .MaximumLength(256);
}

// ── Handlers ───────────────────────────────────────────────────────────────────

public sealed class MemberHandlers(
    IAppDbContext db,
    ICurrentUser currentUser,
    ListAccess access,
    IIdentityService identity) :
    IRequestHandler<GetMembersQuery, IReadOnlyList<MemberDto>>,
    IRequestHandler<InviteMemberCommand, MemberDto>,
    IRequestHandler<RemoveMemberCommand, Unit>
{
    public async Task<IReadOnlyList<MemberDto>> Handle(GetMembersQuery request, CancellationToken ct)
    {
        var userId = currentUser.RequireUserId();
        var list = await access.RequireMembershipAsync(request.ListId, userId, ct);
        return await access.ProjectMembersAsync(list, userId, ct);
    }

    public async Task<MemberDto> Handle(InviteMemberCommand request, CancellationToken ct)
    {
        var userId = currentUser.RequireUserId();
        var list = await access.RequireOwnershipAsync(request.ListId, userId, ct);

        var email = request.Email.Trim().ToLowerInvariant();
        var existingAccount = await identity.FindByEmailAsync(email, ct);

        if (existingAccount is not null && list.HasMember(existingAccount.Id))
        {
            throw new ConflictException($"{email} is already on this list.");
        }

        // Someone with an account joins immediately; everyone else waits as an invitation.
        var member = list.Invite(email);
        if (existingAccount is not null)
        {
            member.Accept(existingAccount.Id);
        }

        await db.SaveChangesAsync(ct);

        var projected = await access.ProjectMembersAsync(list, userId, ct);
        return projected.First(m => m.Id == member.Id);
    }

    public async Task<Unit> Handle(RemoveMemberCommand request, CancellationToken ct)
    {
        var userId = currentUser.RequireUserId();
        var list = await access.RequireOwnershipAsync(request.ListId, userId, ct);

        list.RemoveMember(request.MemberId);
        await db.SaveChangesAsync(ct);
        return Unit.Value;
    }
}

/// <summary>
/// Binds every pending invitation addressed to an email onto the account that just
/// claimed it. Called on sign-in and registration, so an invited person sees the
/// shared list the first time they land on the home screen.
/// </summary>
public sealed record ClaimInvitationsCommand(Guid UserId, string Email) : IRequest<int>;

public sealed class ClaimInvitationsHandler(IAppDbContext db) : IRequestHandler<ClaimInvitationsCommand, int>
{
    public async Task<int> Handle(ClaimInvitationsCommand request, CancellationToken ct)
    {
        var email = request.Email.Trim().ToLowerInvariant();

        var pending = db.ListMembers
            .Where(m => m.InvitedEmail == email && m.Status == MembershipStatus.Invited)
            .ToList();

        foreach (var member in pending)
        {
            member.Accept(request.UserId);
        }

        if (pending.Count > 0)
        {
            await db.SaveChangesAsync(ct);
        }

        return pending.Count;
    }
}
