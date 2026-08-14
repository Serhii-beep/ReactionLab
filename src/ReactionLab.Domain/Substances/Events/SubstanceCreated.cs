using ReactionLab.Domain.Common;

namespace ReactionLab.Domain.Substances.Events;

public sealed record SubstanceCreated(SubstanceId SubstanceId, ChemicalFormula Formula) : DomainEvent;
