using MediatR;
using Microsoft.AspNetCore.Mvc;
using ReactionLab.Application.DTOs;
using ReactionLab.Application.Features.Elements.Commands;
using ReactionLab.Application.Features.Elements.Queries;

namespace ReactionLab.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ElementsController : ControllerBase
{
    private readonly IMediator _mediator;

    public ElementsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<ElementSummaryDto>>> GetAll(CancellationToken cancellationToken)
    {
        var elements = await _mediator.Send(new GetAllElementsQuery(), cancellationToken);

        return Ok(elements);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ElementDto>> GetById(Guid id, CancellationToken cancellationToken)
    {
        var element = await _mediator.Send(new GetElementByIdQuery(id), cancellationToken);

        if (element is null)
        {
            return NotFound();
        }

        return Ok(element);
    }

    [HttpGet("symbol/{symbol}")]
    public async Task<ActionResult<ElementDto>> GetBySymbol(string symbol, CancellationToken cancellationToken)
    {
        var element = await _mediator.Send(new GetElementBySymbolQuery(symbol), cancellationToken);

        if (element is null)
        {
            return NotFound();
        }

        return Ok(element);
    }

    [HttpGet("search")]
    public async Task<ActionResult<IReadOnlyList<ElementSummaryDto>>> Search([FromQuery] string q, CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(q))
        {
            return BadRequest("Search term is required");
        }

        var elements = await _mediator.Send(new SearchElementsQuery(q), cancellationToken);

        return Ok(elements);
    }

    [HttpPost]
    public async Task<ActionResult<ElementDto>> Create(CreateElementDto dto, CancellationToken cancellationToken)
    {
        var element = await _mediator.Send(new CreateElementCommand(dto), cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = element.Id }, element);
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<ElementDto>> Update(Guid id, UpdateElementDto dto, CancellationToken cancellationToken)
    {
        var element = await _mediator.Send(new UpdateElementCommand(id, dto), cancellationToken);

        if (element is null)
        {
            return NotFound();
        }

        return Ok(element);
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new DeleteElementCommand(id), cancellationToken);

        if (!result)
        {
            return NotFound();
        }

        return NoContent();
    }
}