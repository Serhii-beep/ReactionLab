import { ReactionSummary } from "./reaction";

export interface MissingReactant {
    readonly substanceId: string;
    readonly formula: string;
    readonly name: string;
    readonly required: number;
    readonly available: number;
}

export interface Readiness {
    readonly runnable: boolean;
    readonly missing: readonly MissingReactant[];
}

interface RequiredReactant {
    readonly formula: string;
    readonly name: string;
    readonly required: number;
}

export function readiness(reaction: ReactionSummary, bench: ReadonlyMap<string, number>): Readiness {
    const required = requiredReactants(reaction);

    if (required.size === 0) {
        return { runnable: false, missing: [] };
    }

    const missing: MissingReactant[] = [];

    for (const [substanceId, reactant] of required) {
        const available = bench.get(substanceId) ?? 0;

        if (available < reactant.required) {
            missing.push({ substanceId, formula: reactant.formula, name: reactant.name, required: reactant.required, available });
        }
    }

    return { runnable: missing.length === 0, missing };
}

function requiredReactants(reaction: ReactionSummary): ReadonlyMap<string, RequiredReactant> {
    const totals = new Map<string, RequiredReactant>();

    for (const participant of reaction.participants) {
        if (participant.role !== 'Reactant') {
            continue;
        }

        const existing = totals.get(participant.substanceId);

        totals.set(participant.substanceId, {
            formula: participant.formula,
            name: participant.name,
            required: (existing?.required ?? 0) + participant.coefficient
        });
    }

    return totals;
}