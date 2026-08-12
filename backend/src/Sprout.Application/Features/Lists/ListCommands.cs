using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Sprout.Application.Common.Abstractions;
using Sprout.Application.Common.Contracts;
using Sprout.Application.Common.Exceptions;
using Sprout.Application.Common.Services;
using Sprout.Domain.Lists;

namespace Sprout.Application.Features.Lists;

// ── Requests ───────────────────────────────────────────────────────────────────

/// <summary>Creates an empty list of the chosen type. An empty name falls back to "Untitled list".</summary>
public sealed record CreateListCommand(string Name, Guid ListTypeId) : IRequest<TodoListDetailDto>;

public sealed record RenameListCommand(Guid ListId, string Name) : IRequest<Unit>;

/// <summary>Deletes a list and everything on it. Owner only.</summary>
public sealed record DeleteListCommand(Guid ListId) : IRequest<Unit>;

/// <summary>Stores the caller's sort choice for one list. Per member, per list.</summary>
public sealed record SetListSortCommand(Guid ListId, SortMode Sort) : IRequest<Unit>;

/// <summary>Stores whether the caller has the completed section expanded on one list.</summary>
public sealed record SetShowCompletedCommand(Guid ListId, bool ShowCompleted) : IRequest<Unit>;

// ── Validators ─────────────────────────────────────────────────────────────────

public sealed class CreateListCommandValidator : AbstractValidator<CreateListCommand>
{
    public CreateListCommandValidator()
    {
        // Name is deliberately not required: the design falls back to "Untitled list".
        RuleFor(x => x.Name).MaximumLength(120).WithMessage("Keep the name under 120 characters.");
        RuleFor(x => x.ListTypeId).NotEmpty().WithMessage("Choose a list type.");
    }
}

public sealed class RenameListCommandValidator : AbstractValidator<RenameListCommand>
{
    public RenameListCommandValidator() =>
        RuleFor(x => x.Name).NotEmpty().WithMessage("Give the list a name.").MaximumLength(120);
}

// ── Handlers ───────────────────────────────────────────────────────────────────

public sealed class ListCommandHandlers(
    IAppDbContext db,
    ICurrentUser currentUser,
    ListAccess access,
    IMediator mediator) :
    IRequestHandler<CreateListCommand, TodoListDetailDto>,
    IRequestHandler<RenameListCommand, Unit>,
    IRequestHandler<DeleteListCommand, Unit>,
    IRequestHandler<SetListSortCommand, Unit>,
    IRequestHandler<SetShowCompletedCommand, Unit>
{
    public async Task<TodoListDetailDto> Handle(CreateListCommand request, CancellationToken ct)
    {
        var userId = currentUser.RequireUserId();

        var typeExists = await db.ListTypes
            .AnyAsync(t => t.Id == request.ListTypeId && t.OwnerId == userId, ct);

        if (!typeExists)
        {
            throw new NotFoundException("That list type");
        }

        var list = TodoList.Create(userId, request.Name, request.ListTypeId);
        db.TodoLists.Add(list);
        await db.SaveChangesAsync(ct);

        // The design navigates straight into the new list, so hand back the full detail.
        return await mediator.Send(new GetListQuery(list.Id), ct);
    }

    public async Task<Unit> Handle(RenameListCommand request, CancellationToken ct)
    {
        var userId = currentUser.RequireUserId();
        var list = await access.RequireOwnershipAsync(request.ListId, userId, ct);

        list.Rename(request.Name);
        await db.SaveChangesAsync(ct);
        return Unit.Value;
    }

    public async Task<Unit> Handle(DeleteListCommand request, CancellationToken ct)
    {
        var userId = currentUser.RequireUserId();
        var list = await access.RequireOwnershipAsync(request.ListId, userId, ct);

        db.TodoLists.Remove(list);
        await db.SaveChangesAsync(ct);
        return Unit.Value;
    }

    public async Task<Unit> Handle(SetListSortCommand request, CancellationToken ct)
    {
        var userId = currentUser.RequireUserId();
        var list = await access.RequireMembershipAsync(request.ListId, userId, ct);

        list.MemberFor(userId)!.SetSort(request.Sort);
        await db.SaveChangesAsync(ct);
        return Unit.Value;
    }

    public async Task<Unit> Handle(SetShowCompletedCommand request, CancellationToken ct)
    {
        var userId = currentUser.RequireUserId();
        var list = await access.RequireMembershipAsync(request.ListId, userId, ct);

        list.MemberFor(userId)!.SetShowCompleted(request.ShowCompleted);
        await db.SaveChangesAsync(ct);
        return Unit.Value;
    }
}
