import { PredictedParticipant, PredictedReaction, StandardState } from "../../../core/chemistry/chemistry-contracts";
import { ReactionSummary } from "../../../data/reactions/reaction";
import { EquationTerm } from "../../../design-system/chemistry/chem-equation-side";
import { ChemicalState } from "../../../design-system/chemistry/chem-formula";
import { BadgeTone } from "../../../design-system/primitives/badge/badge";
import { KELVIN_AT_ZERO_CELSIUS } from "../../../state/lab-conditions-store";

export interface PredictedReactionView {
    readonly signature: string;
    readonly reactants: readonly EquationTerm[];
    readonly products: readonly EquationTerm[];
    readonly ruleKey: string;
    readonly confidencePercent: number;
    readonly confidenceTone: BadgeTone;
    readonly minimumCelsius: number | null;
    readonly enthalpySign: '+' | '-';
    readonly enthalpyMagnitudeKilojoulesPerMole: number | null;
    readonly activationEnergyKilojoulesPerMole: number | null;
}

interface SignatureTerm {
    readonly formula: string;
    readonly coefficient: number;
}

const HIGH_CONFIDENCE = 0.85;
const MEDIUM_CONFIDENCE = 0.5;

const STATE_SYMBOLS: Readonly<Record<StandardState, ChemicalState>> = {
    Solid: 's',
    Liquid: 'l',
    Gas: 'g',
    Aqueous: 'aq'
};

export function predictedReactionViews(
    predicted: readonly PredictedReaction[],
    catalog: readonly ReactionSummary[]
): readonly PredictedReactionView[] {
    const known = new Set(catalog.map((reaction) => equationSignature(
        reaction.participants.filter((participant) => participant.role === 'Reactant'),
        reaction.participants.filter((participant) => participant.role === 'Product'))));

    return predicted.map(viewOf).filter((view) => !known.has(view.signature));
}

export function equationSignature(reactants: readonly SignatureTerm[], products: readonly SignatureTerm[]): string {
    const side = (terms: readonly SignatureTerm[]) =>
        terms.map(({ formula, coefficient }) => `${coefficient} ${formula}`).sort().join(' + ');

    return `${side(reactants)} -> ${side(products)}`;
}

function viewOf(reaction: PredictedReaction): PredictedReactionView {
    return {
        signature: equationSignature(reaction.reactants, reaction.products),
        reactants: reaction.reactants.map(termOf),
        products: reaction.products.map(termOf),
        ruleKey: reaction.rule,
        confidencePercent: Math.round(100 * reaction.confidence),
        confidenceTone: toneOf(reaction.confidence),
        minimumCelsius: reaction.minimumKelvin === undefined ? null : Math.round(reaction.minimumKelvin - KELVIN_AT_ZERO_CELSIUS),
        enthalpySign: (reaction.enthalpyKilojoulesPerMole ?? 0) < 0 ? '-' : '+',
        enthalpyMagnitudeKilojoulesPerMole: rounded(reaction.enthalpyKilojoulesPerMole === undefined ? undefined : Math.abs(reaction.enthalpyKilojoulesPerMole)),
        activationEnergyKilojoulesPerMole: rounded(reaction.activationEnergyKilojoulesPerMole)
    };
}

function termOf(participant: PredictedParticipant): EquationTerm {
    return {
        formula: participant.formula,
        coefficient: participant.coefficient,
        state: participant.phase === undefined ? undefined : STATE_SYMBOLS[participant.phase]
    };
}

function toneOf(confidence: number): BadgeTone {
    if (confidence >= HIGH_CONFIDENCE) {
        return 'success';
    }

    return confidence >= MEDIUM_CONFIDENCE ? 'warning' : 'neutral';
}

function rounded(kilojoulesPerMole: number | undefined): number | null {
    return kilojoulesPerMole === undefined ? null : Math.round(kilojoulesPerMole);
}
