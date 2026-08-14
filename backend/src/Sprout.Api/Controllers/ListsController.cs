using MediatR;
using Microsoft.AspNetCore.Mvc;
using Sprout.Api.Contracts;
using Sprout.Application.Common.Contracts;
using Sprout.Application.Features.Items;
using Sprout.Application.Features.Lists;
using Sprout.Application.Features.Members;

namespace Sprout.Api.Controllers;

/// <summary>
/// Lists, their items and their members. Items and members are nested under their
/// list because neither is addressable on its own: access is always decided by
/// membership of the list.
/// </summary>
[Route("api/lists")]
public sealed class ListsController(ISender mediator) : ApiControllerBase(mediator)
{
    // ── Lists ──────────────────────────────────────────────────────────────────

    /// <summary>Every list the caller owns or is a member of, as cards for the home screen.</summary>
    [HttpGet]
    [ProducesResponseType<IReadOnlyList<TodoListSummaryDto>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<TodoListSummaryDto>>> GetAll(CancellationToken ct) =>
        Ok(await Mediator.Send(new GetListsQuery(), ct));

    /// <summary>
    /// One list with its type, categories, items in the caller's chosen order,
    /// and members: everything the list detail screen renders.
    /// </summary>
    [HttpGet("{listId:guid}")]
    [ProducesResponseType<TodoListDetailDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<TodoListDetailDto>> Get(Guid listId, CancellationToken ct) =>
        Ok(await Mediator.Send(new GetListQuery(listId), ct));

    /// <summary>
    /// Creates an empty list of the chosen type and returns it in full, since the
    /// design navigates straight into it. An empty name becomes "Untitled list".
    /// </summary>
    [HttpPost]
    [ProducesResponseType<TodoListDetailDto>(StatusCodes.Status201Created)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<TodoListDetailDto>> Create(CreateListRequest request, CancellationToken ct)
    {
        var list = await Mediator.Send(new CreateListCommand(request.Name, request.ListTypeId), ct);
        return CreatedAtAction(nameof(Get), new { listId = list.Id }, list);
    }

    /// <summary>Renames a list. Owner only.</summary>
    [HttpPut("{listId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> Rename(Guid listId, RenameListRequest request, CancellationToken ct)
    {
        await Mediator.Send(new RenameListCommand(listId, request.Name), ct);
        return NoContent();
    }

    /// <summary>Deletes a list and everything on it. Owner only.</summary>
    [HttpDelete("{listId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> Delete(Guid listId, CancellationToken ct)
    {
        await Mediator.Send(new DeleteListCommand(listId), ct);
        return NoContent();
    }

    /// <summary>Stores the caller's sort choice for this list. Per member, per list.</summary>
    [HttpPut("{listId:guid}/sort")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> SetSort(Guid listId, SetSortRequest request, CancellationToken ct)
    {
        await Mediator.Send(new SetListSortCommand(listId, request.Sort), ct);
        return NoContent();
    }

    /// <summary>Stores whether the caller has the completed section expanded on this list.</summary>
    [HttpPut("{listId:guid}/show-completed")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> SetShowCompleted(
        Guid listId,
        SetShowCompletedRequest request,
        CancellationToken ct)
    {
        await Mediator.Send(new SetShowCompletedCommand(listId, request.ShowCompleted), ct);
        return NoContent();
    }

    // ── Items ──────────────────────────────────────────────────────────────────

    /// <summary>
    /// Appends an open item. A null category falls back to the type's first
    /// category, which is what the Add item sheet pre-selects.
    /// </summary>
    [HttpPost("{listId:guid}/items")]
    [ProducesResponseType<TodoItemDto>(StatusCodes.Status201Created)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<TodoItemDto>> AddItem(
        Guid listId,
        AddItemRequest request,
        CancellationToken ct)
    {
        var item = await Mediator.Send(
            new AddItemCommand(listId, request.Text, request.CategoryId, request.DueOn), ct);

        return CreatedAtAction(nameof(Get), new { listId }, item);
    }

    /// <summary>
    /// Flips an item between open and completed. Shared state: every member sees
    /// the change on their next read.
    /// </summary>
    [HttpPost("{listId:guid}/items/{itemId:guid}/toggle")]
    [ProducesResponseType<TodoItemDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<TodoItemDto>> ToggleItem(Guid listId, Guid itemId, CancellationToken ct) =>
        Ok(await Mediator.Send(new ToggleItemCommand(listId, itemId), ct));

    [HttpPut("{listId:guid}/items/{itemId:guid}")]
    [ProducesResponseType<TodoItemDto>(StatusCodes.Status200OK)]
    public async Task<ActionResult<TodoItemDto>> UpdateItem(
        Guid listId,
        Guid itemId,
        UpdateItemRequest request,
        CancellationToken ct) =>
        Ok(await Mediator.Send(
            new UpdateItemCommand(listId, itemId, request.Text, request.CategoryId, request.DueOn), ct));

    /// <summary>Sets how many of an item. Any member may change it; the floor is 1.</summary>
    [HttpPut("{listId:guid}/items/{itemId:guid}/quantity")]
    [ProducesResponseType<TodoItemDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<TodoItemDto>> SetItemQuantity(
        Guid listId,
        Guid itemId,
        SetItemQuantityRequest request,
        CancellationToken ct) =>
        Ok(await Mediator.Send(new SetItemQuantityCommand(listId, itemId, request.Quantity), ct));

    [HttpDelete("{listId:guid}/items/{itemId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> DeleteItem(Guid listId, Guid itemId, CancellationToken ct)
    {
        await Mediator.Send(new DeleteItemCommand(listId, itemId), ct);
        return NoContent();
    }

    // ── Members ────────────────────────────────────────────────────────────────

    /// <summary>The Shared with screen: owner, then editors, then pending invitations.</summary>
    [HttpGet("{listId:guid}/members")]
    [ProducesResponseType<IReadOnlyList<MemberDto>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<MemberDto>>> GetMembers(Guid listId, CancellationToken ct) =>
        Ok(await Mediator.Send(new GetMembersQuery(listId), ct));

    /// <summary>
    /// Invites someone by email. If that address already has an account they join
    /// immediately; otherwise the invitation stays pending until they sign up.
    /// </summary>
    [HttpPost("{listId:guid}/members")]
    [ProducesResponseType<MemberDto>(StatusCodes.Status201Created)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<MemberDto>> InviteMember(
        Guid listId,
        InviteMemberRequest request,
        CancellationToken ct)
    {
        var member = await Mediator.Send(new InviteMemberCommand(listId, request.Email), ct);
        return CreatedAtAction(nameof(GetMembers), new { listId }, member);
    }

    /// <summary>Removes a member or withdraws an invitation. Owner only, and never the owner.</summary>
    [HttpDelete("{listId:guid}/members/{memberId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> RemoveMember(Guid listId, Guid memberId, CancellationToken ct)
    {
        await Mediator.Send(new RemoveMemberCommand(listId, memberId), ct);
        return NoContent();
    }
}
