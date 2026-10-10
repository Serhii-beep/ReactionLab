import { ReactionParticipant, ReactionSummary } from "../../../data/reactions/reaction";
import { GasLookName } from "../../../engine/gas/gas-look";

const OXYGEN = 'O2';
const WATER = 'H2O';
const CARBON_DIOXIDE = 'CO2';
const CLEAN_FLAME_PRODUCTS: ReadonlySet<string> = new Set([CARBON_DIOXIDE, WATER]);
const STEAM_REACTANTS: ReadonlySet<string> = new Set(['H2', OXYGEN, 'O3']);
const CARBON_ATOM = /C(?![a-z])/;

export function gasLookOf(reaction: ReactionSummary): GasLookName | null {
    const reactants = reaction.participants.filter((participant) => participant.role === 'Reactant');
    const productFormulas = reaction.participants.filter((participant) => participant.role === 'Product').map((product) => product.formula);

    if (reaction.enthalpyKilojoulesPerMole === null || reactants.some((reactant) => reactant.state === 'Aqueous')) {
        return null;
    }

    if (productFormulas.every((formula) => formula === WATER) && reactants.every((reactant) => STEAM_REACTANTS.has(reactant.formula))) {
        return 'steam';
    }

    return burnsClean(reactants, productFormulas) ? 'clean-flame' : null;
}

function burnsClean(reactants: readonly ReactionParticipant[], productFormulas: readonly string[]): boolean {
    return productFormulas.includes(CARBON_DIOXIDE)
        && productFormulas.every((formula) => CLEAN_FLAME_PRODUCTS.has(formula))
        && reactants.some((reactant) => reactant.formula === OXYGEN)
        && reactants.every((reactant) => reactant.formula === OXYGEN || isCarbonCompound(reactant.formula));
}

function isCarbonCompound(formula: string): boolean {
    return formula !== 'C' && CARBON_ATOM.test(formula);
}
