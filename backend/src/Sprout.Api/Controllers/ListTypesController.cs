using MediatR;
using Microsoft.AspNetCore.Mvc;
using Sprout.Api.Contracts;
using Sprout.Application.Common.Contracts;
using Sprout.Application.Features.ListTypes;
using ValidationException = Sprout.Application.Common.Exceptions.ValidationException;

namespace Sprout.Api.Controllers;

/// <summary>
/// List types and the categories they own. A type's category order is the custom
/// sort for every list of that type, so the reorder routes here change what the
/// list detail screen renders.
/// </summary>
[Route("api/list-types")]
public sealed class ListTypesController(ISender mediator) : ApiControllerBase(mediator)
{
    /// <summary>Every type the caller owns, with categories and how many lists use each.</summary>
    [HttpGet]
    [ProducesResponseType<IReadOnlyList<ListTypeDto>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<ListTypeDto>>> GetAll(CancellationToken ct) =>
        Ok(await Mediator.Send(new GetListTypesQuery(), ct));

    /// <summary>One type with its ordered categories.</summary>
    [HttpGet("{listTypeId:guid}")]
    [ProducesResponseType<ListTypeDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ListTypeDto>> Get(Guid listTypeId, CancellationToken ct) =>
        Ok(await Mediator.Send(new GetListTypeQuery(listTypeId), ct));

    /// <summary>Creates a type, seeded with a single "Uncategorised" category.</summary>
    [HttpPost]
    [ProducesResponseType<ListTypeDto>(StatusCodes.Status201Created)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ListTypeDto>> Create(CreateListTypeRequest request, CancellationToken ct)
    {
        var type = await Mediator.Send(new CreateListTypeCommand(request.Name, request.Blurb), ct);
        return CreatedAtAction(nameof(Get), new { listTypeId = type.Id }, type);
    }

    [HttpPut("{listTypeId:guid}")]
    [ProducesResponseType<ListTypeDto>(StatusCodes.Status200OK)]
    public async Task<ActionResult<ListTypeDto>> Rename(
        Guid listTypeId,
        RenameListTypeRequest request,
        CancellationToken ct) =>
        Ok(await Mediator.Send(new RenameListTypeCommand(listTypeId, request.Name), ct));

    /// <summary>Deletes a type. Refused with 409 while any list still uses it.</summary>
    [HttpDelete("{listTypeId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Delete(Guid listTypeId, CancellationToken ct)
    {
        await Mediator.Send(new DeleteListTypeCommand(listTypeId), ct);
        return NoContent();
    }

    /// <summary>Appends a category, which takes the next colour in the palette cycle.</summary>
    [HttpPost("{listTypeId:guid}/categories")]
    [ProducesResponseType<ListTypeDto>(StatusCodes.Status200OK)]
    public async Task<ActionResult<ListTypeDto>> AddCategory(
        Guid listTypeId,
        CategoryNameRequest request,
        CancellationToken ct) =>
        Ok(await Mediator.Send(new AddCategoryCommand(listTypeId, request.Name), ct));

    [HttpPut("{listTypeId:guid}/categories/{categoryId:guid}")]
    [ProducesResponseType<ListTypeDto>(StatusCodes.Status200OK)]
    public async Task<ActionResult<ListTypeDto>> RenameCategory(
        Guid listTypeId,
        Guid categoryId,
        CategoryNameRequest request,
        CancellationToken ct) =>
        Ok(await Mediator.Send(new RenameCategoryCommand(listTypeId, categoryId, request.Name), ct));

    /// <summary>
    /// Deletes a category, moving any items that used it to the type's catch-all
    /// first. The type's last category cannot be deleted.
    /// </summary>
    [HttpDelete("{listTypeId:guid}/categories/{categoryId:guid}")]
    [ProducesResponseType<ListTypeDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ListTypeDto>> DeleteCategory(
        Guid listTypeId,
        Guid categoryId,
        CancellationToken ct) =>
        Ok(await Mediator.Send(new DeleteCategoryCommand(listTypeId, categoryId), ct));

    /// <summary>
    /// Moves a category one place up or down. Up on the first row and down on the
    /// last are no-ops, and return the type unchanged.
    /// </summary>
    [HttpPost("{listTypeId:guid}/categories/{categoryId:guid}/move")]
    [ProducesResponseType<ListTypeDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ListTypeDto>> MoveCategory(
        Guid listTypeId,
        Guid categoryId,
        MoveCategoryRequest request,
        CancellationToken ct)
    {
        if (!request.IsValid)
        {
            throw ValidationException.ForField("direction", "Direction must be \"up\" or \"down\".");
        }

        return Ok(await Mediator.Send(new MoveCategoryCommand(listTypeId, categoryId, request.IsUp), ct));
    }
}
