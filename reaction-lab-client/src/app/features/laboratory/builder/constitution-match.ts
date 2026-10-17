export interface ConstitutionBond {
    readonly from: number;
    readonly to: number;
}

export interface Constitution {
    readonly symbols: readonly string[];
    readonly bonds: readonly ConstitutionBond[];
}

interface HeavyAtomGraph {
    readonly labels: readonly string[];
    readonly neighbors: readonly (readonly number[])[];
    readonly bondCount: number;
}

const HYDROGEN = 'H';

export function sameConstitution(first: Constitution, second: Constitution): boolean {
    if (first.symbols.length !== second.symbols.length || first.bonds.length !== second.bonds.length) {
        return false;
    }

    const left = heavyAtomGraphOf(first);
    const right = heavyAtomGraphOf(second);

    return left.bondCount === right.bondCount
        && [...left.labels].sort().join() === [...right.labels].sort().join()
        && mapsOnto(left, right);
}

export function isConnected(atomCount: number, bonds: readonly ConstitutionBond[]): boolean {
    const neighbors = Array.from({ length: atomCount }, (): number[] => []);
    const reached = new Set<number>(atomCount > 0 ? [0] : []);

    for (const { from, to } of bonds) {
        neighbors[from].push(to);
        neighbors[to].push(from);
    }

    for (const atom of reached) {
        for (const neighbor of neighbors[atom]) {
            reached.add(neighbor);
        }
    }

    return reached.size === atomCount;
}

function heavyAtomGraphOf({ symbols, bonds }: Constitution): HeavyAtomGraph {
    const heavy = symbols.flatMap((symbol, index) => (symbol === HYDROGEN ? [] : [index]));
    const positionOf = new Map(heavy.map((atom, position) => [atom, position]));
    const hydrogens = heavy.map(() => 0);
    const neighbors = heavy.map((): number[] => []);
    let bondCount = 0;

    for (const { from, to } of bonds) {
        const fromPosition = positionOf.get(from);
        const toPosition = positionOf.get(to);

        if (fromPosition !== undefined && toPosition !== undefined) {
            neighbors[fromPosition].push(toPosition);
            neighbors[toPosition].push(fromPosition);
            bondCount++;
        } else if (fromPosition !== undefined || toPosition !== undefined) {
            hydrogens[fromPosition ?? toPosition ?? 0]++;
        }
    }

    return { labels: heavy.map((atom, position) => `${symbols[atom]}${hydrogens[position]}`), neighbors, bondCount };
}

function mapsOnto(left: HeavyAtomGraph, right: HeavyAtomGraph): boolean {
    const order = searchOrderOf(left);
    const image = new Array<number>(left.labels.length).fill(-1);
    const taken = new Array<boolean>(right.labels.length).fill(false);
    const adjacent = right.neighbors.map((around) => new Set(around));

    const fits = (atom: number, candidate: number) =>
        !taken[candidate]
        && left.labels[atom] === right.labels[candidate]
        && left.neighbors[atom].length === right.neighbors[candidate].length
        && left.neighbors[atom].every((neighbor) => image[neighbor] < 0 || adjacent[candidate].has(image[neighbor]));

    const place = (step: number): boolean => {
        if (step === order.length) {
            return true;
        }

        const atom = order[step];

        for (const candidate of candidatesFor(atom, left, right, image)) {
            if (fits(atom, candidate)) {
                image[atom] = candidate;
                taken[candidate] = true;

                if (place(step + 1)) {
                    return true;
                }

                image[atom] = -1;
                taken[candidate] = false;
            }
        }

        return false;
    };

    return place(0);
}

function candidatesFor(atom: number, left: HeavyAtomGraph, right: HeavyAtomGraph, image: readonly number[]): readonly number[] {
    const mappedNeighbor = left.neighbors[atom].find((neighbor) => image[neighbor] >= 0);

    return mappedNeighbor === undefined
        ? right.labels.map((_, index) => index)
        : right.neighbors[image[mappedNeighbor]];
}

function searchOrderOf(graph: HeavyAtomGraph): readonly number[] {
    const order: number[] = [];
    const visited = new Set<number>();

    for (let start = 0; start < graph.labels.length; start++) {
        if (!visited.has(start)) {
            visited.add(start);
            order.push(start);

            for (let head = order.length - 1; head < order.length; head++) {
                for (const neighbor of graph.neighbors[order[head]].filter((next) => !visited.has(next))) {
                    visited.add(neighbor);
                    order.push(neighbor);
                }
            }
        }
    }

    return order;
}
