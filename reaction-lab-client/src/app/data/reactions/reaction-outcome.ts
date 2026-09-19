import { SubstanceSummary } from "../substances/substance";
import { ParticipantRole, ReactionSummary } from "./reaction";
import { MissingReactant } from "./reaction-readiness";

export interface SubstancePortion {
    readonly substance: SubstanceSummary;
    readonly count: number;
}

export interface ReactantsConsumption {
    readonly substanceId: string;
    readonly count: number;
}

export interface ReactionOutcome {
    readonly consumed: readonly ReactantsConsumption[];
    readonly produced: readonly SubstancePortion[];
}

export function productIdsOf(reaction: ReactionSummary): readonly string[] {
    return [...coefficientTotals(reaction, 'Product').keys()];
}

export function outcomeOf(reaction: ReactionSummary, products: readonly SubstanceSummary[]): ReactionOutcome {
    const productsById = new Map(products.map((substance) => [substance.id, substance]));
    const produced: SubstancePortion[] = [];

    for (const [substanceId, count] of coefficientTotals(reaction, 'Product')) {
        const substance = productsById.get(substanceId);

        if (substance) {
            produced.push({ substance, count });
        }
    }

    return {
        consumed: [...coefficientTotals(reaction, 'Reactant')].map(([substanceId, count]) => ({ substanceId, count })),
        produced
    };
}

export function missingPortions(missing: readonly MissingReactant[], substances: readonly SubstanceSummary[]): readonly SubstancePortion[] {
    const substancesById = new Map(substances.map((substance) => [substance.id, substance]));
    const portions: SubstancePortion[] = [];

    for (const reactant of missing) {
        const substance = substancesById.get(reactant.substanceId);

        if (substance) {
            portions.push({ substance, count: reactant.required - reactant.available });
        }
    }

    return portions;
}

function coefficientTotals(reaction: ReactionSummary, role: ParticipantRole): ReadonlyMap<string, number> {
    const totals = new Map<string, number>();

    for (const participant of reaction.participants) {
        if (participant.role === role) {
            totals.set(participant.substanceId, (totals.get(participant.substanceId) ?? 0) + participant.coefficient);
        }
    }

    return totals;
}