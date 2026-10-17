import { Raycaster, Sphere, Vector2, Vector3 } from "three";
import { EngineContext } from "../core/engine-context";
import { PlacedAtom, PlacedBond } from "../scene/bench-layout";

export class PickingService {
    private readonly raycaster = new Raycaster();
    private readonly pointer = new Vector2();
    private readonly sphere = new Sphere();
    private readonly hit = new Vector3();

    constructor(private readonly context: EngineContext) {}

    pick(atoms: readonly PlacedAtom[], clientX: number, clientY: number): PlacedAtom | null {
        if (!this.aim(clientX, clientY)) {
            return null;
        }

        const { ray } = this.raycaster;
        let nearest: PlacedAtom | null = null;
        let best = Infinity;

        for (const atom of atoms) {
            this.sphere.set(atom.position, atom.radius);

            if (ray.intersectSphere(this.sphere, this.hit)) {
                const distance = this.hit.distanceToSquared(ray.origin);

                if (distance < best) {
                    best = distance;
                    nearest = atom;
                }
            }
        }

        return nearest;
    }

    pickBond(bonds: readonly PlacedBond[], clientX: number, clientY: number, reach: number): PlacedBond | null {
        if (!this.aim(clientX, clientY)) {
            return null;
        }

        const { ray } = this.raycaster;
        let nearest: PlacedBond | null = null;
        let best = Infinity;

        for (const bond of bonds) {
            if (ray.distanceSqToSegment(bond.from.position, bond.to.position, this.hit) <= reach * reach) {
                const distance = this.hit.distanceToSquared(ray.origin);

                if (distance < best) {
                    best = distance;
                    nearest = bond;
                }
            }
        }

        return nearest;
    }

    private aim(clientX: number, clientY: number): boolean {
        const rect = this.context.canvas.getBoundingClientRect();

        if (rect.width === 0 || rect.height === 0) {
            return false;
        }

        this.pointer.set(((clientX - rect.left) / rect.width) * 2 - 1, -((clientY - rect.top) / rect.height) * 2 + 1);
        this.raycaster.setFromCamera(this.pointer, this.context.camera);

        return true;
    }
}
