import { Vector3 } from "three";
import { CrystalStructure } from "./crystal-structures";
import { IonCount, IonicFormula, radiusAngstromOf } from "./ionic-formula";
import { IonRole } from "./ion-table";

export interface LatticeIon {
    readonly ion: IonCount;
    readonly role: IonRole;
    readonly position: Vector3;
}

type HoleSet = 'octahedral' | 'octahedral-layers' | 'tetrahedral' | 'tetrahedral-alternate' | 'cubic';

type GridIndex = readonly [number, number, number];

type SitesByRole = Readonly<Record<IonRole, readonly Vector3[]>>;

interface StructurePlan {
    readonly frameworkRole: IonRole;
    readonly holeSets: readonly HoleSet[];
}

interface UnitSlots {
    readonly sites: SitesByRole;
    readonly taken: Set<Vector3>;
    readonly center: Vector3;
}

const PLANS: Readonly<Record<CrystalStructure, StructurePlan>> = {
    'rock-salt': { frameworkRole: 'anion', holeSets: ['octahedral'] },
    'cesium-chloride': { frameworkRole: 'anion', holeSets: ['cubic'] },
    'zinc-blende': { frameworkRole: 'anion', holeSets: ['tetrahedral-alternate'] },
    'fluorite': { frameworkRole: 'cation', holeSets: ['tetrahedral'] },
    'antifluorite': { frameworkRole: 'anion', holeSets: ['tetrahedral'] },
    'layered': { frameworkRole: 'anion', holeSets: ['octahedral-layers'] },
    'corundum': { frameworkRole: 'anion', holeSets: ['octahedral'] },
    'aluminum-chloride': { frameworkRole: 'anion', holeSets: ['octahedral'] },
    'anti-bixbyite': { frameworkRole: 'anion', holeSets: ['tetrahedral'] },
    'filled-fluorite': { frameworkRole: 'anion', holeSets: ['tetrahedral', 'octahedral'] }
};
const SITES_PER_SLOT = 8;
const TIE_SHARE = 1e-3;
const DISTANCE_ROUNDING = 1e4;

export function crystalliteOf(formula: IonicFormula, structure: CrystalStructure, unitCount: number): LatticeIon[][] {
    const plan = PLANS[structure];
    const step = stepOf(plan, contactAngstromOf(formula));
    const center = new Vector3(step / 2, step / 2, step / 2);
    const slots: UnitSlots = { sites: sitesOf(plan, step, reachFor(formula, unitCount), center), taken: new Set(), center };

    return Array.from({ length: unitCount }, () => assignUnit(formula, slots));
}

function assignUnit(formula: IonicFormula, slots: UnitSlots): LatticeIon[] {
    const cations = expanded(formula.cations);
    const anions = expanded(formula.anions);
    const seedRole: IonRole = cations.length <= anions.length ? 'cation' : 'anion';
    const partnerRole: IonRole = seedRole === 'cation' ? 'anion' : 'cation';
    const [seeds, partners] = seedRole === 'cation' ? [cations, anions] : [anions, cations];
    const growthCenter = slots.taken.size === 0 ? slots.center : centroidOf([...slots.taken]);
    const first = takeNearest(slots, seedRole, growthCenter, growthCenter);
    const seedPlaces = [first, ...seeds.slice(1).map(() => takeNearest(slots, seedRole, first, growthCenter))];
    const seedCentroid = centroidOf(seedPlaces);
    const partnerPlaces = partners.map(() => takeNearest(slots, partnerRole, seedCentroid, growthCenter));
    const seeded = seeds.map((ion, index) => ({ ion, role: seedRole, position: seedPlaces[index].clone() }));
    const partnered = partners.map((ion, index) => ({ ion, role: partnerRole, position: partnerPlaces[index].clone() }));

    return seedRole === 'cation' ? [...seeded, ...partnered] : [...partnered, ...seeded];
}

function takeNearest(slots: UnitSlots, role: IonRole, target: Vector3, tieTarget: Vector3): Vector3 {
    let nearest: Vector3 | null = null;
    let nearestScore = Infinity;

    for (const site of slots.sites[role]) {
        const score = slots.taken.has(site) ? Infinity : site.distanceTo(target) + TIE_SHARE * site.distanceTo(tieTarget);

        if (score < nearestScore) {
            nearest = site;
            nearestScore = score;
        }
    }

    if (nearest === null) {
        throw new Error(`No free ${role} site left in the crystallite`);
    }

    slots.taken.add(nearest);

    return nearest;
}

function sitesOf(plan: StructurePlan, step: number, reach: number, center: Vector3): SitesByRole {
    const simpleCubic = plan.holeSets.includes('cubic');
    const indices = gridIndicesWithin(reach);
    const framework = indices.filter((index) => simpleCubic || parityOf(index) === 0).map((index) => pointAt(index, 0, step));
    const holes = plan.holeSets.flatMap((holeSet) => holesOf(holeSet, indices, step));
    const frameworkSites = sortedFrom(framework, center);
    const holeSites = sortedFrom(holes, center);

    return plan.frameworkRole === 'anion' ? { anion: frameworkSites, cation: holeSites } : { cation: frameworkSites, anion: holeSites };
}

function holesOf(holeSet: HoleSet, indices: readonly GridIndex[], step: number): Vector3[] {
    switch (holeSet) {
        case 'octahedral':
            return indices.filter((index) => parityOf(index) === 1).map((index) => pointAt(index, 0, step));
        case 'octahedral-layers':
            return indices.filter((index) => parityOf(index) === 1 && Math.abs(index[2]) % 2 === 0).map((index) => pointAt(index, 0, step));
        case 'tetrahedral':
        case 'cubic':
            return indices.map((index) => pointAt(index, 0.5, step));
        case 'tetrahedral-alternate':
            return indices.filter((index) => parityOf(index) === 0).map((index) => pointAt(index, 0.5, step));
    }
}

function stepOf(plan: StructurePlan, contactAngstrom: number): number {
    const tetrahedralContact = plan.holeSets.some((holeSet) => holeSet !== 'octahedral' && holeSet !== 'octahedral-layers');

    return tetrahedralContact ? (2 * contactAngstrom) / Math.sqrt(3) : contactAngstrom;
}

function contactAngstromOf({ cations, anions }: IonicFormula): number {
    return meanRadiusOf(cations) + meanRadiusOf(anions);
}

function meanRadiusOf(ions: readonly IonCount[]): number {
    return ions.reduce((sum, ion) => sum + radiusAngstromOf(ion) * ion.count, 0) / ionCountOf(ions);
}

function ionCountOf(ions: readonly IonCount[]): number {
    return ions.reduce((sum, ion) => sum + ion.count, 0);
}

function reachFor({ cations, anions }: IonicFormula, unitCount: number): number {
    const ionsPerRole = Math.max(ionCountOf(cations), ionCountOf(anions));

    return Math.ceil(Math.cbrt(SITES_PER_SLOT * ionsPerRole * unitCount) / 2) + 1;
}

function gridIndicesWithin(reach: number): GridIndex[] {
    const span = Array.from({ length: 2 * reach + 1 }, (_, index) => index - reach);

    return span.flatMap((x) => span.flatMap((y) => span.map((z) => [x, y, z] as const)));
}

function parityOf([x, y, z]: GridIndex): number {
    return Math.abs(x + y + z) % 2;
}

function pointAt([x, y, z]: GridIndex, shift: number, step: number): Vector3 {
    return new Vector3(x + shift, y + shift, z + shift).multiplyScalar(step);
}

function sortedFrom(sites: readonly Vector3[], center: Vector3): Vector3[] {
    const shellOf = (site: Vector3) => Math.round(Math.max(Math.abs(site.x - center.x), Math.abs(site.y - center.y), Math.abs(site.z - center.z)) * DISTANCE_ROUNDING);
    const distanceOf = (site: Vector3) => Math.round(site.distanceTo(center) * DISTANCE_ROUNDING);

    return [...sites].sort((first, second) => shellOf(first) - shellOf(second) || first.y - second.y || distanceOf(first) - distanceOf(second) || first.x - second.x || first.z - second.z);
}

function expanded(ions: readonly IonCount[]): IonCount[] {
    return ions.flatMap((ion) => Array<IonCount>(ion.count).fill(ion));
}

function centroidOf(points: readonly Vector3[]): Vector3 {
    return points.reduce((sum, point) => sum.add(point), new Vector3()).divideScalar(Math.max(points.length, 1));
}
