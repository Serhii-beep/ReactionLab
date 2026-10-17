using ReactionLab.SharedKernel;

namespace ReactionLab.Catalog.Domain.Substances.Events;

public sealed record SubstanceCreated(SubstanceId SubstanceId, ChemicalFormula Formula) : DomainEvent;
