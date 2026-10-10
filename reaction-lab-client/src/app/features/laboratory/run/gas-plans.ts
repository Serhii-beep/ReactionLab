import { ReactionParticipant, ReactionSummary } from "../../../data/reactions/reaction";
import { SubstanceDetail } from "../../../data/substances/substance";
import { GasPlan, PrecipitatePlan } from "../../../engine/animation/reaction-script";
import { kelvinHeatedBy } from "../../../engine/animation/energy-ledger";
import { parseHillFormula } from "../scene/hill-formula";
import { isWaterBorne } from "./reaction-energetics";
import { solidColorOf } from "./solid-colors";

type DetailsById = ReadonlyMap<string, SubstanceDetail>;

interface MetalVaporization {
    readonly boilingKelvin: number;
    readonly vaporizationKilojoulesPerMole: number;
}

const OXYGEN = 'O2';
const WATER = 'H2O';
const CARBON = 'C';
const CARBON_DIOXIDE = 'CO2';
const CLEAN_FLAME_PRODUCTS: ReadonlySet<string> = new Set([CARBON_DIOXIDE, WATER]);
const STEAM_REACTANTS: ReadonlySet<string> = new Set(['H2', OXYGEN, 'O3']);
const SOOT_LOADING_BY_PRODUCT_FORMULA: ReadonlyMap<string, number> = new Map([[CARBON, 0.4], ['CO', 0.15]]);
const SMOKE_LOADING = 0.3;
const CARBON_ATOM = /C(?![a-z])/;
const VAPORIZATION_BY_METAL: ReadonlyMap<string, MetalVaporization> = new Map([
    ['Li', { boilingKelvin: 1615, vaporizationKilojoulesPerMole: 147.1 }],
    ['Na', { boilingKelvin: 1156, vaporizationKilojoulesPerMole: 97.4 }],
    ['K', { boilingKelvin: 1032, vaporizationKilojoulesPerMole: 76.9 }],
    ['Mg', { boilingKelvin: 1363, vaporizationKilojoulesPerMole: 128 }],
    ['Ca', { boilingKelvin: 1757, vaporizationKilojoulesPerMole: 154.7 }],
    ['Al', { boilingKelvin: 2792, vaporizationKilojoulesPerMole: 294 }],
    ['Zn', { boilingKelvin: 1180, vaporizationKilojoulesPerMole: 115.3 }],
    ['Pb', { boilingKelvin: 2022, vaporizationKilojoulesPerMole: 179.5 }],
    ['Fe', { boilingKelvin: 3134, vaporizationKilojoulesPerMole: 340 }],
    ['Cu', { boilingKelvin: 2835, vaporizationKilojoulesPerMole: 300.4 }],
    ['Ag', { boilingKelvin: 2435, vaporizationKilojoulesPerMole: 250.6 }]
]);
const STEAM: GasPlan = { look: 'steam', particles: null };

export function gasPlanOf(reaction: ReactionSummary, details: DetailsById, precipitate: PrecipitatePlan | null): GasPlan | null {
    const reactants = participantsOf(reaction, 'Reactant');
    const products = participantsOf(reaction, 'Product');

    if (reactants.some(isWaterBorne)) {
        return precipitate === null ? null : precipitateCloudOf(precipitate, details);
    }

    return reaction.enthalpyKilojoulesPerMole === null ? null : heatedPlanOf(reaction, reactants, products, details);
}

function heatedPlanOf(reaction: ReactionSummary, reactants: readonly ReactionParticipant[], products: readonly ReactionParticipant[], details: DetailsById): GasPlan | null {
    if (products.every((product) => product.formula === WATER) && reactants.every((reactant) => STEAM_REACTANTS.has(reactant.formula))) {
        return STEAM;
    }

    return (burns(reactants) ? combustionPlanOf(products) : null)
        ?? ionicSmokeOf(reaction, reactants, products, details)
        ?? (products.some((product) => product.formula === WATER && product.state === 'Gas') ? STEAM : null);
}

function precipitateCloudOf(precipitate: PrecipitatePlan, details: DetailsById): GasPlan {
    const formula = details.get(precipitate.substanceId)?.formula ?? '';

    return { look: 'precipitate', particles: { substanceIds: [precipitate.substanceId], seedSymbol: null, albedo: solidColorOf(formula), loading: 1 } };
}

function burns(reactants: readonly ReactionParticipant[]): boolean {
    return reactants.some((reactant) => reactant.formula === OXYGEN)
        && reactants.every((reactant) => reactant.formula === OXYGEN || (reactant.formula !== CARBON && CARBON_ATOM.test(reactant.formula)));
}

function combustionPlanOf(products: readonly ReactionParticipant[]): GasPlan | null {
    const formulas = products.map((product) => product.formula);
    const soot = products.filter((product) => SOOT_LOADING_BY_PRODUCT_FORMULA.has(product.formula));

    if (formulas.includes(CARBON_DIOXIDE) && formulas.every((formula) => CLEAN_FLAME_PRODUCTS.has(formula))) {
        return { look: 'clean-flame', particles: null };
    }

    return soot.length === 0 ? null : {
        look: 'sooty-flame',
        particles: {
            substanceIds: soot.map((product) => product.substanceId),
            seedSymbol: CARBON,
            albedo: solidColorOf(CARBON),
            loading: Math.max(...soot.map((product) => SOOT_LOADING_BY_PRODUCT_FORMULA.get(product.formula) ?? 0))
        }
    };
}

function ionicSmokeOf(reaction: ReactionSummary, reactants: readonly ReactionParticipant[], products: readonly ReactionParticipant[], details: DetailsById): GasPlan | null {
    const solids = products.filter((product) => product.state === 'Solid' && details.get(product.substanceId)?.kind === 'Ionic');
    const fromGases = reactants.every((reactant) => reactant.state === 'Gas');

    if (solids.length === 0 || (reaction.enthalpyKilojoulesPerMole ?? 0) >= 0 || !(fromGases || burnsAsVapor(reaction, reactants, products, details))) {
        return null;
    }

    return {
        look: 'ionic-smoke',
        particles: { substanceIds: solids.map((product) => product.substanceId), seedSymbol: null, albedo: solidColorOf(solids[0].formula), loading: SMOKE_LOADING }
    };
}

function burnsAsVapor(reaction: ReactionSummary, reactants: readonly ReactionParticipant[], products: readonly ReactionParticipant[], details: DetailsById): boolean {
    const metal = reactants.find((reactant) => VAPORIZATION_BY_METAL.has(reactant.formula));
    const vaporization = metal === undefined ? undefined : VAPORIZATION_BY_METAL.get(metal.formula);

    if (metal === undefined || vaporization === undefined) {
        return false;
    }

    const heatKilojoules = -(reaction.enthalpyKilojoulesPerMole ?? 0);
    const leftKilojoules = heatKilojoules - metal.coefficient * vaporization.vaporizationKilojoulesPerMole;
    const productAtoms = products.reduce((sum, product) => sum + product.coefficient * atomCountOf(details.get(product.substanceId)), 0);

    return kelvinHeatedBy(leftKilojoules, productAtoms) >= vaporization.boilingKelvin;
}

function atomCountOf(detail: SubstanceDetail | undefined): number {
    return detail === undefined ? 0 : parseHillFormula(detail.hillFormula).reduce((sum, { count }) => sum + count, 0);
}

function participantsOf(reaction: ReactionSummary, role: ReactionParticipant['role']): ReactionParticipant[] {
    return reaction.participants.filter((participant) => participant.role === role);
}
