using MediatR;
using Microsoft.AspNetCore.Mvc;
using ReactionLab.Application.Common.Pagination;
using ReactionLab.Application.DTOs;
using ReactionLab.Application.Features.Reactions.Commands;
using ReactionLab.Application.Features.Reactions.Queries;
using ReactionLab.Domain.Enums;

namespace ReactionLab.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ReactionsController : ControllerBase
{
    private readonly IMediator _mediator;

    public ReactionsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<ReactionSummaryDto>>> GetAll(CancellationToken cancellationToken)
    {
        var reactions = await _mediator.Send(new GetAllReactionsQuery(), cancellationToken);
        return Ok(reactions);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ReactionDto>> GetById(Guid id, CancellationToken cancellationToken)
    {
        var reaction = await _mediator.Send(new GetReactionByIdQuery(id), cancellationToken);

        if (reaction is null)
        {
            return NotFound();
        }

        return Ok(reaction);
    }

    [HttpGet("type/{type}")]
    public async Task<ActionResult<IReadOnlyList<ReactionSummaryDto>>> GetByType(ReactionType type, CancellationToken cancellationToken)
    {
        var reactions = await _mediator.Send(new GetReactionsByTypeQuery(type), cancellationToken);
        return Ok(reactions);
    }

    [HttpGet("search")]
    public async Task<ActionResult<IReadOnlyList<ReactionSummaryDto>>> Search([FromQuery] string q, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(q))
        {
            return BadRequest("Search term is required");
        }

        var reactions = await _mediator.Send(new SearchReactionsQuery(q), cancellationToken);
        return Ok(reactions);
    }

    [HttpPost("find")]
    public async Task<ActionResult<IReadOnlyList<ReactionSummaryDto>>> FindByReactants([FromBody] FindReactantsRequest request, CancellationToken cancellationToken)
    {
        var reactions = await _mediator.Send(new FindReactionsByReactantsQuery(request.ElementIds ?? [], request.MoleculeIds ?? []), cancellationToken);

        return Ok(reactions);
    }

    [HttpPost("available")]
    public async Task<ActionResult<CursorPagedResult<ReactionSummaryDto>>> FindAvailable([FromBody] FindAvailableReactionsRequest request, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new FindAvailableReactionsQuery(
            request.MoleculeIds ?? [],
            request.ElementIds ?? [],
            request.SearchTerm,
            request.PageSize,
            request.Cursor), cancellationToken);

        return Ok(result);
    }

    [HttpPost]
    public async Task<ActionResult<ReactionDto>> Create(CreateReactionDto dto, CancellationToken cancellationToken)
    {
        var reaction = await _mediator.Send(new CreateReactionCommand(dto), cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = reaction.Id }, reaction);
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<ReactionDto>> Update(Guid id, UpdateReactionDto dto, CancellationToken cancellationToken)
    {
        var reaction = await _mediator.Send(new UpdateReactionCommand(id, dto), cancellationToken);

        if (reaction is null)
        {
            return NotFound();
        }

        return Ok(reaction);
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new DeleteReactionCommand(id), cancellationToken);

        if (!result)
        {
            return NotFound();
        }

        return NoContent();
    }
}

public record FindReactantsRequest(IEnumerable<Guid>? ElementIds, IEnumerable<Guid>? MoleculeIds);

public record FindAvailableReactionsRequest(
    IEnumerable<Guid>? MoleculeIds,
    IEnumerable<Guid>? ElementIds,
    string? SearchTerm = null,
    int PageSize = 20,
    string? Cursor = null
);
