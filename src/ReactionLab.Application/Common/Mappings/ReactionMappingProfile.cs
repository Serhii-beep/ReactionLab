using AutoMapper;
using ReactionLab.Application.DTOs;
using ReactionLab.Domain.Entities;
using ReactionLab.Domain.Enums;

namespace ReactionLab.Application.Common.Mappings;

public class ReactionMappingProfile : Profile
{
    public ReactionMappingProfile()
    {
        CreateMap<Reaction, ReactionDto>()
            .ForMember(dest => dest.Reactants, opt => opt.MapFrom(src =>
                src.Participants.Where(p => p.Role == ParticipantRole.Reactant)))
            .ForMember(dest => dest.Products, opt => opt.MapFrom(src =>
                src.Participants.Where(p => p.Role == ParticipantRole.Product)))
            .ForMember(dest => dest.Tags, opt => opt.MapFrom(src =>
                src.ReactionTags.Select(rt => rt.Tag.Name)));

        CreateMap<Reaction, ReactionSummaryDto>();

        CreateMap<ReactionParticipant, ReactionParticipantDto>()
            .ForMember(dest => dest.ElementSymbol, opt => opt.MapFrom(src =>
                src.Element != null ? src.Element.Symbol : null))
            .ForMember(dest => dest.ElementName, opt => opt.MapFrom(src =>
                src.Element != null ? src.Element.Name : null))
            .ForMember(dest => dest.MoleculeFormula, opt => opt.MapFrom(src =>
                src.Molecule != null ? src.Molecule.Formula : null))
            .ForMember(dest => dest.MoleculeName, opt => opt.MapFrom(src =>
                src.Molecule != null ? src.Molecule.Name : null));

        CreateMap<CreateReactionDto, Reaction>()
            .ForMember(dest => dest.Participants, opt => opt.Ignore())
            .ForMember(dest => dest.ReactionTags, opt => opt.Ignore());

        CreateMap<UpdateReactionDto, Reaction>()
            .ForAllMembers(opts => opts.Condition((src, dest, srcMember) => srcMember != null));
    }
}