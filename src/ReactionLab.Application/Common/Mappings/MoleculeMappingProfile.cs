using AutoMapper;
using ReactionLab.Application.DTOs;
using ReactionLab.Domain.Entities;

namespace ReactionLab.Application.Common.Mappings;

public class MoleculeMappingProfile : Profile
{
    public MoleculeMappingProfile()
    {
        CreateMap<Molecule, MoleculeDto>()
            .ForMember(dest => dest.Elements, opt => opt.MapFrom(src => src.MoleculeElements));

        CreateMap<Molecule, MoleculeSummaryDto>();

        CreateMap<MoleculeElement, MoleculeElementDto>()
            .ForMember(dest => dest.Symbol, opt => opt.MapFrom(src => src.Element.Symbol))
            .ForMember(dest => dest.Name, opt => opt.MapFrom(src => src.Element.Name));

        CreateMap<CreateMoleculeDto, Molecule>()
            .ForMember(dest => dest.MoleculeElements, opt => opt.Ignore());

        CreateMap<UpdateMoleculeDto, Molecule>()
            .ForAllMembers(opts => opts.Condition((src, dest, srcMember) => srcMember != null));
    }
}