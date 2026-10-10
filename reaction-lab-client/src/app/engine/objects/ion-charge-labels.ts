import { Group, PerspectiveCamera, Vector3 } from "three";
import { Text } from "troika-three-text";
import { Disposable } from "../core/disposal-scope";
import { PlacedAtom } from "../scene/bench-layout";
import { LabelAtlas } from "../resources/label-atlas";
import { projectedRadius } from "../core/projection";
import { groundHaloOf, LabelInk } from "./label-ink";

export interface ChargeLabelSizing {
    readonly sizePerRadius: number;
    readonly minimumPixels: number;
}

interface ChargedIon {
    readonly atoms: readonly PlacedAtom[];
    readonly charge: number;
}

const SIZE_SHARE = 0.68;
const RIM_SHARE = 0.78;
const OUTLINE_WIDTH = '9%';
const MINUS = '−';

export class IonChargeLabels implements Disposable {
    readonly root = new Group();

    private readonly pool: Text[] = [];
    private readonly labelsAwaitingText = new Set<Text>();
    private readonly center = new Vector3();
    private readonly toCamera = new Vector3();
    private readonly cameraRight = new Vector3();
    private readonly cameraUp = new Vector3();
    private ions: readonly ChargedIon[] = [];

    constructor(private readonly atlas: LabelAtlas, private readonly sizing: ChargeLabelSizing) {
        this.root.name = 'ion-charge-labels';
    }

    render(atoms: readonly PlacedAtom[], ink: LabelInk): void {
        this.ions = chargedIonsOf(atoms);

        this.ions.forEach((ion, index) => {
            const label = index < this.pool.length ? this.pool[index] : this.grow(ink);
            const text = chargeTextOf(ion.charge);

            if (label.text !== text) {
                label.text = text;
                this.labelsAwaitingText.add(label);
            }

            label.fontSize = this.sizing.sizePerRadius * SIZE_SHARE;
            inkLabel(label, ink);
            label.visible = !this.labelsAwaitingText.has(label);
            label.sync(() => this.labelsAwaitingText.delete(label));
        });

        for (const label of this.pool.slice(this.ions.length)) {
            label.visible = false;
        }
    }

    refreshInk(ink: LabelInk): void {
        this.ions.forEach((_, index) => inkLabel(this.pool[index], ink));
    }

    update(camera: PerspectiveCamera, viewportHeight: number): void {
        this.cameraRight.set(1, 0, 0).applyQuaternion(camera.quaternion);
        this.cameraUp.set(0, 1, 0).applyQuaternion(camera.quaternion);

        this.ions.forEach((ion, index) => {
            const label = this.pool[index];
            const reach = this.measure(ion);
            const largestRadius = Math.max(...ion.atoms.map((atom) => atom.radius));
            const distance = this.toCamera.copy(camera.position).sub(this.center).length();

            label.visible = !this.labelsAwaitingText.has(label) && projectedRadius(largestRadius, distance, camera.fov, viewportHeight) >= this.sizing.minimumPixels;

            if (label.visible) {
                label.position.copy(this.center)
                    .addScaledVector(this.cameraRight, reach * RIM_SHARE)
                    .addScaledVector(this.cameraUp, reach * RIM_SHARE)
                    .addScaledVector(this.toCamera.divideScalar(distance), reach);
                label.scale.setScalar(largestRadius);
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

    private measure(ion: ChargedIon): number {
        this.center.set(0, 0, 0);

        for (const atom of ion.atoms) {
            this.center.add(atom.position);
        }

        this.center.divideScalar(ion.atoms.length);

        return ion.atoms.reduce((reach, atom) => Math.max(reach, atom.position.distanceTo(this.center) + atom.radius), 0);
    }

    private grow(ink: LabelInk): Text {
        const label = this.atlas.create('', { size: 1, color: ink.dark });

        label.outlineWidth = OUTLINE_WIDTH;
        this.pool.push(label);
        this.root.add(label);

        return label;
    }
}

function chargedIonsOf(atoms: readonly PlacedAtom[]): ChargedIon[] {
    const atomsByIon = new Map<string, PlacedAtom[]>();

    for (const atom of atoms) {
        if (atom.ion !== null) {
            const key = `${atom.unitId}|${atom.ion.index}`;

            atomsByIon.set(key, [...(atomsByIon.get(key) ?? []), atom]);
        }
    }

    return [...atomsByIon.values()].map((ionAtoms) => ({ atoms: ionAtoms, charge: ionAtoms[0].ion?.charge ?? 0 }));
}

function chargeTextOf(charge: number): string {
    const sign = charge > 0 ? '+' : MINUS;
    const magnitude = Math.abs(charge);

    return magnitude === 1 ? sign : `${magnitude}${sign}`;
}

function inkLabel(label: Text, ink: LabelInk): void {
    label.color = ink.ground.getHex();
    label.outlineColor = groundHaloOf(ink).getHex();
}
