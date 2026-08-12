using Microsoft.EntityFrameworkCore;
using Sprout.Application.Common.Abstractions;
using Sprout.Application.Common.Contracts;
using Sprout.Application.Common.Exceptions;
using Sprout.Domain.Lists;

namespace Sprout.Application.Common.Services;

/// <summary>
/// The one place that answers "may this person touch this list?", plus the member
/// projection every list-facing screen needs. Every list handler goes through here
/// so the rule cannot drift between endpoints.
/// </summary>
public sealed class ListAccess(IAppDbContext db, IIdentityService identity)
{
    /// <summary>
    /// Loads a list the caller is an active member of, with its type, categories,
    /// items and members attached. Non-members get a 404 rather than a 403, so the
    /// API never confirms that a list they cannot see exists.
    /// </summary>
    public async Task<TodoList> RequireMembershipAsync(Guid listId, Guid userId, CancellationToken ct)
    {
        var list = await db.TodoLists
            .Include(l => l.ListType!).ThenInclude(t => t.Categories)
            .Include(l => l.Items)
            .Include(l => l.Members)
            .FirstOrDefaultAsync(l => l.Id == listId, ct);

        if (list is null || !list.HasMember(userId))
        {
            throw new NotFoundException("That list");
        }

        return list;
    }

    /// <summary>As <see cref="RequireMembershipAsync"/>, but also insists the caller owns the list.</summary>
    public async Task<TodoList> RequireOwnershipAsync(Guid listId, Guid userId, CancellationToken ct)
    {
        var list = await RequireMembershipAsync(listId, userId, ct);
        return list.OwnerId == userId
            ? list
            : throw new ForbiddenException("Only the list owner can do that.");
    }

    /// <summary>
    /// Projects memberships into rows for the Shared with screen and the avatar
    /// stacks, resolving account profiles in a single lookup. Owner first, then
    /// active editors, then pending invitations.
    /// </summary>
    public async Task<IReadOnlyList<MemberDto>> ProjectMembersAsync(
        TodoList list,
        Guid actingUserId,
        CancellationToken ct)
    {
        var userIds = list.Members.Where(m => m.UserId is not null).Select(m => m.UserId!.Value);
        var profiles = await identity.GetProfilesAsync(userIds, ct);

        return
        [
            .. list.Members
                .OrderBy(m => m.Role == ListRole.Owner ? 0 : m.Status == MembershipStatus.Active ? 1 : 2)
                .ThenBy(m => m.CreatedAt)
                .Select(m => ToDto(m, profiles, actingUserId))
        ];
    }

    private static MemberDto ToDto(
        ListMember member,
        IReadOnlyDictionary<Guid, UserProfile> profiles,
        Guid actingUserId)
    {
        var profile = member.UserId is { } id && profiles.TryGetValue(id, out var found) ? found : null;

        // A pending invitation has no account yet, so it shows the email address
        // where a name would go and derives its initials from that.
        var email = profile?.Email ?? member.InvitedEmail;
        var displayName = profile?.DisplayName ?? member.InvitedEmail ?? "Invited";

        return new MemberDto(
            member.Id,
            member.UserId,
            displayName,
            email,
            profile?.Initials ?? InitialsFrom(displayName),
            profile?.AvatarColor ?? AvatarColorFor(displayName),
            member.Role.ToString(),
            member.Status.ToString(),
            member.UserId == actingUserId);
    }

    /// <summary>Up to two letters, taken from the first two words, or the first two characters.</summary>
    public static string InitialsFrom(string value)
    {
        var source = value.Split('@')[0];
        var words = source.Split([' ', '.', '-', '_'], StringSplitOptions.RemoveEmptyEntries);

        var initials = words.Length >= 2
            ? $"{words[0][0]}{words[1][0]}"
            : source.Length >= 2 ? source[..2] : source;

        return initials.ToUpperInvariant();
    }

    /// <summary>
    /// A stable avatar colour from the design's member palette, chosen by hashing the
    /// identifier so the same person keeps the same circle across devices.
    /// </summary>
    public static string AvatarColorFor(string identifier)
    {
        string[] palette = ["#c67139", "#7a8a5e", "#82796a", "#b2622d", "#56633f", "#f6a06b"];
        var hash = identifier.Aggregate(17, (acc, ch) => unchecked((acc * 31) + ch));
        return palette[Math.Abs(hash) % palette.Length];
    }
}
