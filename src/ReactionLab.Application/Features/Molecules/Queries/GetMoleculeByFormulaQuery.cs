using AutoMapper;
using MediatR;
using ReactionLab.Application.DTOs;
using ReactionLab.Domain.Interfaces;

namespace ReactionLab.Application.Features.Molecules.Queries;

public record GetMoleculeByFormulaQuery(string Formula) : IRequest<MoleculeSummaryDto?>;

public class GetMoleculeByFormulaQueryHandler : IRequestHandler<GetMoleculeByFormulaQuery, MoleculeSummaryDto?>
{
    private readonly IMoleculeRepository _moleculeRepository;
    private readonly IMapper _mapper;

    public GetMoleculeByFormulaQueryHandler(IMoleculeRepository moleculeRepository, IMapper mapper)
    {
        _moleculeRepository = moleculeRepository;
        _mapper = mapper;
    }

    public async Task<MoleculeSummaryDto?> Handle(GetMoleculeByFormulaQuery request, CancellationToken cancellationToken)
    {
        var molecule = await _moleculeRepository.GetByFormulaAsync(request.Formula, cancellationToken);
        return molecule is null ? null : _mapper.Map<MoleculeSummaryDto>(molecule);
    }
}