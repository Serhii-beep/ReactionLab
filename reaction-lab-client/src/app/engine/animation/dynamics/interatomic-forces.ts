import { FLOOR_CLEARANCE } from "../../scene/bench-layout";
import { addPictureGradient, BondingPicture } from "./bonding-picture";
import { CoreRepulsion } from "./core-repulsion";
import { MolecularSystem, TIME_UNITS_PER_SECOND } from "./molecular-system";

const FLOOR_RATE = 25 / TIME_UNITS_PER_SECOND;
const CRITICAL_DAMPING = 2;

export class InteratomicForces {
    private readonly reactantGradient: Float64Array;
    private readonly productGradient: Float64Array;
    private readonly cores: CoreRepulsion;

    constructor(private readonly system: MolecularSystem, private readonly reactant: BondingPicture, private readonly product: BondingPicture) {
        this.reactantGradient = new Float64Array(system.positions.length);
        this.productGradient = new Float64Array(system.positions.length);
        this.cores = new CoreRepulsion(system, reactant, product);
    }

    accumulate(productWeight: number): void {
        const { positions, forces } = this.system;

        this.reactantGradient.fill(0);
        this.productGradient.fill(0);
        addPictureGradient(this.reactant, positions, this.reactantGradient);
        addPictureGradient(this.product, positions, this.productGradient);

        for (let slot = 0; slot < forces.length; slot++) {
            forces[slot] -= (1 - productWeight) * this.reactantGradient[slot] + productWeight * this.productGradient[slot];
        }

        this.cores.accumulate();
        this.keepAboveFloor();
    }

    private keepAboveFloor(): void {
        const { atoms, positions, velocities, forces } = this.system;

        atoms.forEach((atom, index) => {
            const heightSlot = index * 3 + 1;
            const depth = atom.radius + FLOOR_CLEARANCE - positions[heightSlot];

            if (depth > 0) {
                forces[heightSlot] += atom.mass * (FLOOR_RATE * FLOOR_RATE * depth - CRITICAL_DAMPING * FLOOR_RATE * velocities[heightSlot]);
            }
        });
    }
}