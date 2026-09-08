import { Color, Group, PerspectiveCamera, Vector3 } from "three";
import { Disposable } from "../core/disposal-scope";
import { Text } from "troika-three-text";
import { PlacedAtom } from "../scene/bench-layout";
import { LabelAtlas } from "../resources/label-atlas";
import { projectedRadius } from "../core/projection";

export interface LabelInk {
    readonly dark: Color;
    readonly light: Color;
}

const MIN_PIXELS = 12;
const SIZE_FACTOR = 1.1;
const INK_LUMINANCE = 0.35;
const SURFACE_OFFSET = 1.02;

export class AtomLabels implements Disposable {
    readonly root = new Group();

    private readonly pool: Text[] = [];
    private readonly toCamera = new Vector3();
    private atoms: readonly PlacedAtom[] = [];

    constructor(private readonly atlas: LabelAtlas) {
        this.root.name = 'atom-labels';
    }

    render(atoms: readonly PlacedAtom[], ink: LabelInk): void {
        this.atoms = atoms;

        atoms.forEach((atom, index) => {
            const label = index < this.pool.length ? this.pool[index] : this.grow(ink);

            label.text = atom.symbol;
            label.fontSize = atom.radius * SIZE_FACTOR;
            label.color = inkFor(atom.color, ink).getHex();
            label.visible = true;
            label.sync();
        });

        for (const label of this.pool.slice(atoms.length)) {
            label.visible = false;
        }
    }

    update(camera: PerspectiveCamera, viewportHeight: number): void {
        this.atoms.forEach((atom, index) => {
            const label = this.pool[index];

            this.toCamera.copy(camera.position).sub(atom.position);

            const distance = this.toCamera.length();

            label.visible = projectedRadius(atom.radius, distance, camera.fov, viewportHeight) >= MIN_PIXELS;

            if (label.visible) {
                label.position.copy(atom.position).addScaledVector(this.toCamera.divideScalar(distance), atom.radius * SURFACE_OFFSET);
                label.quaternion.copy(camera.quaternion);
            }
        });
    }

    dispose(): void {
        for (const label of this.pool) {
            this.atlas.release(label);
        }

        this.pool.length = 0;
        this.root.removeFromParent();
    }

    private grow(ink: LabelInk): Text {
        const label = this.atlas.create('', { size: 1, color: ink.dark });

        this.pool.push(label);
        this.root.add(label);

        return label;
    }
}

function inkFor(color: Color, ink: LabelInk): Color {
    const luminance = 0.2126 * color.r + 0.7152 * color.g + 0.0722 * color.b;

    return luminance > INK_LUMINANCE ? ink.dark : ink.light;
}