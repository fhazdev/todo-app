using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Sprout.Application.Common.Abstractions;
using Sprout.Application.Common.Contracts;
using Sprout.Application.Common.Exceptions;
using Sprout.Domain.Categories;

namespace Sprout.Application.Features.ListTypes;

// ── Requests ───────────────────────────────────────────────────────────────────

/// <summary>
/// Creates a type. It is seeded with a single "Uncategorised" category, so the client
/// can navigate straight into its category screen with something already on it.
/// </summary>
public sealed record CreateListTypeCommand(string Name, string? Blurb) : IRequest<ListTypeDto>;

public sealed record RenameListTypeCommand(Guid ListTypeId, string Name) : IRequest<ListTypeDto>;

/// <summary>Deletes a type. Refused while any list still uses it.</summary>
public sealed record DeleteListTypeCommand(Guid ListTypeId) : IRequest<Unit>;

/// <summary>Appends a category, which takes the next colour in the palette cycle.</summary>
public sealed record AddCategoryCommand(Guid ListTypeId, string Name) : IRequest<ListTypeDto>;

public sealed record RenameCategoryCommand(Guid ListTypeId, Guid CategoryId, string Name) : IRequest<ListTypeDto>;

/// <summary>
/// Deletes a category, first moving every item that used it to the type's catch-all
/// (or, failing that, to whichever category ends up first).
/// </summary>
public sealed record DeleteCategoryCommand(Guid ListTypeId, Guid CategoryId) : IRequest<ListTypeDto>;

/// <summary>
/// Moves a category one place. This is the custom sort order, so it immediately
/// re-groups every list of this type.
/// </summary>
public sealed record MoveCategoryCommand(Guid ListTypeId, Guid CategoryId, bool Up) : IRequest<ListTypeDto>;

// ── Validators ─────────────────────────────────────────────────────────────────

public sealed class CreateListTypeCommandValidator : AbstractValidator<CreateListTypeCommand>
{
    public CreateListTypeCommandValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Give the type a name.")
            .MaximumLength(80).WithMessage("Keep the name under 80 characters.");

        RuleFor(x => x.Blurb).MaximumLength(120);
    }
}

public sealed class RenameListTypeCommandValidator : AbstractValidator<RenameListTypeCommand>
{
    public RenameListTypeCommandValidator() =>
        RuleFor(x => x.Name).NotEmpty().WithMessage("Give the type a name.").MaximumLength(80);
}

public sealed class AddCategoryCommandValidator : AbstractValidator<AddCategoryCommand>
{
    public AddCategoryCommandValidator() =>
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Give the category a name.")
            .MaximumLength(60).WithMessage("Keep the name under 60 characters.");
}

public sealed class RenameCategoryCommandValidator : AbstractValidator<RenameCategoryCommand>
{
    public RenameCategoryCommandValidator() =>
        RuleFor(x => x.Name).NotEmpty().WithMessage("Give the category a name.").MaximumLength(60);
}

// ── Handlers ───────────────────────────────────────────────────────────────────

public sealed class ListTypeCommandHandlers(IAppDbContext db, ICurrentUser currentUser) :
    IRequestHandler<CreateListTypeCommand, ListTypeDto>,
    IRequestHandler<RenameListTypeCommand, ListTypeDto>,
    IRequestHandler<DeleteListTypeCommand, Unit>,
    IRequestHandler<AddCategoryCommand, ListTypeDto>,
    IRequestHandler<RenameCategoryCommand, ListTypeDto>,
    IRequestHandler<DeleteCategoryCommand, ListTypeDto>,
    IRequestHandler<MoveCategoryCommand, ListTypeDto>
{
    public async Task<ListTypeDto> Handle(CreateListTypeCommand request, CancellationToken ct)
    {
        var userId = currentUser.RequireUserId();
        await GuardDuplicateNameAsync(userId, request.Name, null, ct);

        var type = ListType.Create(userId, request.Name, request.Blurb);
        db.ListTypes.Add(type);
        await db.SaveChangesAsync(ct);

        return ListTypeDto.From(type);
    }

    public async Task<ListTypeDto> Handle(RenameListTypeCommand request, CancellationToken ct)
    {
        var userId = currentUser.RequireUserId();
        var type = await RequireOwnedTypeAsync(request.ListTypeId, userId, ct);

        await GuardDuplicateNameAsync(userId, request.Name, type.Id, ct);
        type.Rename(request.Name);
        await db.SaveChangesAsync(ct);

        return await ProjectAsync(type, ct);
    }

    public async Task<Unit> Handle(DeleteListTypeCommand request, CancellationToken ct)
    {
        var userId = currentUser.RequireUserId();
        var type = await RequireOwnedTypeAsync(request.ListTypeId, userId, ct);

        var inUse = await db.TodoLists.CountAsync(l => l.ListTypeId == type.Id, ct);
        if (inUse > 0)
        {
            throw new ConflictException(
                $"{type.Name} is still used by {inUse} {(inUse == 1 ? "list" : "lists")}. Move or delete them first.");
        }

        db.ListTypes.Remove(type);
        await db.SaveChangesAsync(ct);
        return Unit.Value;
    }

    public async Task<ListTypeDto> Handle(AddCategoryCommand request, CancellationToken ct)
    {
        var userId = currentUser.RequireUserId();
        var type = await RequireOwnedTypeAsync(request.ListTypeId, userId, ct);

        type.AddCategory(request.Name);
        await db.SaveChangesAsync(ct);

        return await ProjectAsync(type, ct);
    }

    public async Task<ListTypeDto> Handle(RenameCategoryCommand request, CancellationToken ct)
    {
        var userId = currentUser.RequireUserId();
        var type = await RequireOwnedTypeAsync(request.ListTypeId, userId, ct);

        type.RenameCategory(request.CategoryId, request.Name);
        await db.SaveChangesAsync(ct);

        return await ProjectAsync(type, ct);
    }

    public async Task<ListTypeDto> Handle(DeleteCategoryCommand request, CancellationToken ct)
    {
        var userId = currentUser.RequireUserId();
        var type = await RequireOwnedTypeAsync(request.ListTypeId, userId, ct);

        // Clear the items before the category goes, so none is left pointing at a row
        // that no longer exists. They become uncategorised rather than being shuffled
        // into a category nobody chose for them.
        var stranded = await db.TodoItems
            .Where(i => i.CategoryId == request.CategoryId)
            .ToListAsync(ct);

        foreach (var item in stranded)
        {
            item.Edit(item.Text, null, item.DueOn);
        }

        type.RemoveCategory(request.CategoryId);
        await db.SaveChangesAsync(ct);

        return await ProjectAsync(type, ct);
    }

    public async Task<ListTypeDto> Handle(MoveCategoryCommand request, CancellationToken ct)
    {
        var userId = currentUser.RequireUserId();
        var type = await RequireOwnedTypeAsync(request.ListTypeId, userId, ct);

        if (request.Up)
        {
            type.MoveCategoryUp(request.CategoryId);
        }
        else
        {
            type.MoveCategoryDown(request.CategoryId);
        }

        await db.SaveChangesAsync(ct);
        return await ProjectAsync(type, ct);
    }

    private async Task<ListType> RequireOwnedTypeAsync(Guid listTypeId, Guid userId, CancellationToken ct) =>
        await db.ListTypes
            .Include(t => t.Categories)
            .FirstOrDefaultAsync(t => t.Id == listTypeId && t.OwnerId == userId, ct)
        ?? throw new NotFoundException("That list type");

    private async Task GuardDuplicateNameAsync(Guid userId, string name, Guid? excludingId, CancellationToken ct)
    {
        var trimmed = name.Trim();
        var clash = await db.ListTypes.AnyAsync(
            t => t.OwnerId == userId
                 && t.Id != excludingId
                 && t.Name.ToLower() == trimmed.ToLower(),
            ct);

        if (clash)
        {
            throw new ConflictException($"You already have a type called \"{trimmed}\".");
        }
    }

    private async Task<ListTypeDto> ProjectAsync(ListType type, CancellationToken ct) =>
        ListTypeDto.From(type, await db.TodoLists.CountAsync(l => l.ListTypeId == type.Id, ct));
}
