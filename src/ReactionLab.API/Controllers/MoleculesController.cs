using MediatR;
using Microsoft.AspNetCore.Mvc;
using ReactionLab.Application.DTOs;
using ReactionLab.Application.Features.Molecules.Commands;
using ReactionLab.Application.Features.Molecules.Queries;

namespace ReactionLab.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class MoleculesController : ControllerBase
{
    private readonly IMediator _mediator;

    public MoleculesController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<MoleculeSummaryDto>>> GetAll(CancellationToken cancellationToken)
    {
        var molecules = await _mediator.Send(new GetAllMoleculesQuery(), cancellationToken);
        return Ok(molecules);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<MoleculeDto>> GetById(Guid id, CancellationToken cancellationToken)
    {
        var molecule = await _mediator.Send(new GetMoleculeByIdQuery(id), cancellationToken);

        if (molecule is null)
        {
            return NotFound();
        }

        return Ok(molecule);
    }

    [HttpGet("formula/{formula}")]
    public async Task<ActionResult<MoleculeSummaryDto>> GetByFormula(string formula, CancellationToken cancellationToken)
    {
        var molecule = await _mediator.Send(new GetMoleculeByFormulaQuery(formula), cancellationToken);

        if (molecule is null)
        {
            return NotFound();
        }

        return Ok(molecule);
    }

    [HttpGet("search")]
    public async Task<ActionResult<IReadOnlyList<MoleculeSummaryDto>>> Search([FromQuery] string q, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(q))
        {
            return BadRequest("Search term is required");
        }

        var molecule = await _mediator.Send(new SearchMoleculesQuery(q), cancellationToken);
        return Ok(molecule);
    }

    [HttpPost]
    public async Task<ActionResult<MoleculeDto>> Create(CreateMoleculeDto dto, CancellationToken cancellationToken)
    {
        var molecule = await _mediator.Send(new CreateMoleculeCommand(dto), cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = molecule.Id, molecule });
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<MoleculeDto>> Update(Guid id, UpdateMoleculeDto dto, CancellationToken cancellationToken)
    {
        var molecule = await _mediator.Send(new UpdateMoleculeCommand(id, dto), cancellationToken);

        if (molecule is null)
        {
            return NotFound();
        }

        return Ok(molecule);
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new DeleteMoleculeCommand(id), cancellationToken);

        if (!result)
        {
            return NotFound();
        }

        return NoContent();
    }
}