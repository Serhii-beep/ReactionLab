using AutoMapper;
using ReactionLab.Application.DTOs;
using ReactionLab.Domain.Entities;

namespace ReactionLab.Application.Common.Mappings;

public class ElementMapping : Profile
{
    public ElementMapping()
    {
        CreateMap<Element, ElementDto>();
        CreateMap<Element, ElementSummaryDto>();
        CreateMap<CreateElementDto, Element>();
        CreateMap<UpdateElementDto, Element>()
            .ForAllMembers(opts => opts.Condition((src, dest, srcMember) => srcMember != null));
    }
}