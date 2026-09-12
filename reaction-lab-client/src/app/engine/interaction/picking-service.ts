import { Raycaster, Sphere, Vector2, Vector3 } from "three";
import { EngineContext } from "../core/engine-context";
import { PlacedAtom } from "../scene/bench-layout";

export class PickingService {
    private readonly raycaster = new Raycaster();
    private readonly pointer = new Vector2();
    private readonly sphere = new Sphere();
    private readonly hit = new Vector3();

    constructor(private readonly context: EngineContext) {}

    pick(atoms: readonly PlacedAtom[], clientX: number, clientY: number): PlacedAtom | null {
        const rect = this.context.canvas.getBoundingClientRect();

        if (rect.width === 0 || rect.height === 0) {
            return null;
        }

        this.pointer.set(((clientX - rect.left) / rect.width) * 2 - 1, -((clientY - rect.top) / rect.height) * 2 + 1);
        this.raycaster.setFromCamera(this.pointer, this.context.camera);

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
}