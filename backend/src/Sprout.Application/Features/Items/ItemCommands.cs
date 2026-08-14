using FluentValidation;
using MediatR;
using Sprout.Application.Common.Abstractions;
using Sprout.Application.Common.Contracts;
using Sprout.Application.Common.Services;
using Sprout.Domain.Lists;
using ValidationException = Sprout.Application.Common.Exceptions.ValidationException;

namespace Sprout.Application.Features.Items;

// ── Requests ───────────────────────────────────────────────────────────────────

/// <summary>
/// Appends an open item. A null category leaves the item uncategorised, which is a
/// valid resting state rather than a gap to be filled in.
/// </summary>
public sealed record AddItemCommand(Guid ListId, string Text, Guid? CategoryId, DateOnly? DueOn)
    : IRequest<TodoItemDto>;

/// <summary>Flips an item between open and completed. Both directions are shared state.</summary>
public sealed record ToggleItemCommand(Guid ListId, Guid ItemId) : IRequest<TodoItemDto>;

/// <summary>Edits an item's text, category or due date. A null category clears it.</summary>
public sealed record UpdateItemCommand(Guid ListId, Guid ItemId, string Text, Guid? CategoryId, DateOnly? DueOn)
    : IRequest<TodoItemDto>;

/// <summary>
/// Sets how many of an item. An absolute value rather than a delta, matching the
/// other item edits; on a shared list the last write wins, which is the same deal
/// as renaming.
/// </summary>
public sealed record SetItemQuantityCommand(Guid ListId, Guid ItemId, int Quantity)
    : IRequest<TodoItemDto>;

public sealed record DeleteItemCommand(Guid ListId, Guid ItemId) : IRequest<Unit>;

// ── Validators ─────────────────────────────────────────────────────────────────

public sealed class AddItemCommandValidator : AbstractValidator<AddItemCommand>
{
    public AddItemCommandValidator() =>
        RuleFor(x => x.Text)
            .NotEmpty().WithMessage("Type what needs doing.")
            .MaximumLength(500).WithMessage("Keep it under 500 characters.");
}

public sealed class UpdateItemCommandValidator : AbstractValidator<UpdateItemCommand>
{
    public UpdateItemCommandValidator()
    {
        RuleFor(x => x.Text).NotEmpty().WithMessage("An item needs some text.").MaximumLength(500);
    }
}

public sealed class SetItemQuantityCommandValidator : AbstractValidator<SetItemQuantityCommand>
{
    public SetItemQuantityCommandValidator() =>
        // Caught here as a field error so the client gets a 400 it can attach to the
        // stepper, rather than the domain's 500-shaped complaint.
        RuleFor(x => x.Quantity)
            .GreaterThanOrEqualTo(TodoItem.MinQuantity)
            .WithMessage($"Quantity cannot go below {TodoItem.MinQuantity}.")
            .LessThanOrEqualTo(999).WithMessage("That is more than anyone needs.");
}

// ── Handlers ───────────────────────────────────────────────────────────────────

public sealed class ItemCommandHandlers(IAppDbContext db, ICurrentUser currentUser, ListAccess access) :
    IRequestHandler<AddItemCommand, TodoItemDto>,
    IRequestHandler<ToggleItemCommand, TodoItemDto>,
    IRequestHandler<UpdateItemCommand, TodoItemDto>,
    IRequestHandler<SetItemQuantityCommand, TodoItemDto>,
    IRequestHandler<DeleteItemCommand, Unit>
{
    public async Task<TodoItemDto> Handle(AddItemCommand request, CancellationToken ct)
    {
        var userId = currentUser.RequireUserId();
        var list = await access.RequireMembershipAsync(request.ListId, userId, ct);
        var type = list.ListType!;

        if (request.CategoryId is { } id && type.FindCategory(id) is null)
        {
            throw ValidationException.ForField("categoryId", $"That category is not on {type.Name}.");
        }

        var item = list.AddItem(request.Text, request.CategoryId, request.DueOn, userId);
        await db.SaveChangesAsync(ct);

        return TodoItemDto.From(item);
    }

    public async Task<TodoItemDto> Handle(ToggleItemCommand request, CancellationToken ct)
    {
        var userId = currentUser.RequireUserId();
        var list = await access.RequireMembershipAsync(request.ListId, userId, ct);

        var item = list.RequireItem(request.ItemId);
        item.Toggle(userId);
        list.Touch();
        await db.SaveChangesAsync(ct);

        return TodoItemDto.From(item);
    }

    public async Task<TodoItemDto> Handle(UpdateItemCommand request, CancellationToken ct)
    {
        var userId = currentUser.RequireUserId();
        var list = await access.RequireMembershipAsync(request.ListId, userId, ct);
        var type = list.ListType!;

        if (request.CategoryId is { } id && type.FindCategory(id) is null)
        {
            throw ValidationException.ForField("categoryId", $"That category is not on {type.Name}.");
        }

        var item = list.RequireItem(request.ItemId);
        item.Edit(request.Text, request.CategoryId, request.DueOn);
        list.Touch();
        await db.SaveChangesAsync(ct);

        return TodoItemDto.From(item);
    }

    public async Task<TodoItemDto> Handle(SetItemQuantityCommand request, CancellationToken ct)
    {
        var userId = currentUser.RequireUserId();
        var list = await access.RequireMembershipAsync(request.ListId, userId, ct);

        // Any member, not just the owner: an editor who can add items can say how
        // many of them are wanted.
        var item = list.RequireItem(request.ItemId);
        item.SetQuantity(request.Quantity);
        list.Touch();
        await db.SaveChangesAsync(ct);

        return TodoItemDto.From(item);
    }

    public async Task<Unit> Handle(DeleteItemCommand request, CancellationToken ct)
    {
        var userId = currentUser.RequireUserId();
        var list = await access.RequireMembershipAsync(request.ListId, userId, ct);

        list.RemoveItem(request.ItemId);
        await db.SaveChangesAsync(ct);
        return Unit.Value;
    }
}
