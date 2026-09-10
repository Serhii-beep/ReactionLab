import { Color, Group, PerspectiveCamera, Vector3 } from "three";
import { Disposable } from "../core/disposal-scope";
import { Text } from "troika-three-text";
import { PlacedAtom, PlacedBond } from "../scene/bench-layout";
import { LabelAtlas } from "../resources/label-atlas";
import { projectedRadius } from "../core/projection";

export interface LabelInk {
    readonly dark: Color;
    readonly light: Color;
}

interface Stick {
    readonly direction: Vector3;
    readonly half: number;
}

type Sticks = Map<PlacedAtom, Stick[]>;

const MIN_PIXELS = 12;
const SIZE_FACTOR = 1.1;
const INK_LUMINANCE = 0.35;
const SURFACE_OFFSET = 1.02;
const FOOTPRINT = 0.6;
const STICK_RADIUS = 0.1;
const RAMP = 0.1;

export class AtomLabels implements Disposable {
    readonly root = new Group();

    private readonly pool: Text[] = [];
    private readonly toCamera = new Vector3();
    private readonly cameraUp = new Vector3();
    private atoms: readonly PlacedAtom[] = [];
    private sticks: Sticks = new Map();

    constructor(private readonly atlas: LabelAtlas) {
        this.root.name = 'atom-labels';
    }

    render(atoms: readonly PlacedAtom[], bonds: readonly PlacedBond[], ink: LabelInk): void {
        this.atoms = atoms;
        this.sticks = sticksOf(bonds);

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
        this.cameraUp.set(0, 1, 0).applyQuaternion(camera.quaternion);

        this.atoms.forEach((atom, index) => {
            const label = this.pool[index];

            this.toCamera.copy(camera.position).sub(atom.position);

            const distance = this.toCamera.length();

            label.visible = projectedRadius(atom.radius, distance, camera.fov, viewportHeight) >= MIN_PIXELS;

            if (label.visible) {
                this.toCamera.divideScalar(distance);
                label.position.copy(atom.position).addScaledVector(this.toCamera, this.offsetFor(atom));
                label.up.copy(this.cameraUp);
                label.lookAt(camera.position);
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

    private offsetFor(atom: PlacedAtom): number {
        const base = atom.radius * SURFACE_OFFSET;
        const clearance = FOOTPRINT * atom.radius + STICK_RADIUS;
        let offset = base;

        for (const stick of this.sticks.get(atom) ?? []) {
            const along = stick.direction.dot(this.toCamera);

            if (along <= 0) {
                continue;
            }

            const lateral = stick.half * Math.sqrt(1 - along * along);
            const weight = Math.min(1, Math.max(0, (clearance + RAMP - lateral) / RAMP));
            const reach = stick.half * along + STICK_RADIUS;

            offset = Math.max(offset, base + (reach - base) * weight);
        }

        return offset;
    }

    private grow(ink: LabelInk): Text {
        const label = this.atlas.create('', { size: 1, color: ink.dark });

        this.pool.push(label);
        this.root.add(label);

        return label;
    }
}

function sticksOf(bonds: readonly PlacedBond[]): Sticks {
    const sticks: Sticks = new Map();

    for (const bond of bonds) {
        const direction = bond.to.position.clone().sub(bond.from.position);
        const length = direction.length();

        if (length === 0) {
            continue;
        }

        direction.divideScalar(length);
        stick(sticks, bond.from, { direction, half: length / 2 });
        stick(sticks, bond.to, { direction: direction.clone().negate(), half: length / 2 });
    }

    return sticks;
}

function stick(sticks: Sticks, atom: PlacedAtom, entry: Stick): void {
    const list = sticks.get(atom);

    if (list) {
        list.push(entry);
    } else {
        sticks.set(atom, [entry]);
    }
}

function inkFor(color: Color, ink: LabelInk): Color {
    const luminance = 0.2126 * color.r + 0.7152 * color.g + 0.0722 * color.b;

    return luminance > INK_LUMINANCE ? ink.dark : ink.light;
}