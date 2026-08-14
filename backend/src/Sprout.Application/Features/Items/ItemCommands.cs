using FluentValidation;
using MediatR;
using Sprout.Application.Common.Abstractions;
using Sprout.Application.Common.Contracts;
using Sprout.Application.Common.Services;
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

// ── Handlers ───────────────────────────────────────────────────────────────────

public sealed class ItemCommandHandlers(IAppDbContext db, ICurrentUser currentUser, ListAccess access) :
    IRequestHandler<AddItemCommand, TodoItemDto>,
    IRequestHandler<ToggleItemCommand, TodoItemDto>,
    IRequestHandler<UpdateItemCommand, TodoItemDto>,
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

    public async Task<Unit> Handle(DeleteItemCommand request, CancellationToken ct)
    {
        var userId = currentUser.RequireUserId();
        var list = await access.RequireMembershipAsync(request.ListId, userId, ct);

        list.RemoveItem(request.ItemId);
        await db.SaveChangesAsync(ct);
        return Unit.Value;
    }
}
