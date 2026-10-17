import { CircleGeometry, Color, CylinderGeometry, DoubleSide, Group, Mesh, MeshBasicMaterial, Object3D, Quaternion, Scene, SphereGeometry, Vector3 } from "three";
import { Disposable } from "../core/disposal-scope";

export interface SymmetryAxisGuide {
    readonly direction: Vector3;
    readonly order: number;
}

export interface SymmetryGuideSet {
    readonly center: Vector3;
    readonly radius: number;
    readonly axes: readonly SymmetryAxisGuide[];
    readonly planeNormals: readonly Vector3[];
    readonly inversionCenter: boolean;
}

const AXIS_REACH = 1.45;
const AXIS_THICKNESS = 0.022;
const PRINCIPAL_AXIS_THICKNESS = 0.036;
const PLANE_REACH = 1.2;
const PLANE_OPACITY = 0.14;
const INVERSION_MARKER_RADIUS = 0.07;
const UP = new Vector3(0, 1, 0);
const FACING = new Vector3(0, 0, 1);

export class SymmetryGuides implements Disposable {
    readonly root = new Group();

    private readonly axisMaterial = new MeshBasicMaterial({ toneMapped: false });
    private readonly planeMaterial = new MeshBasicMaterial({
        toneMapped: false,
        transparent: true,
        opacity: PLANE_OPACITY,
        side: DoubleSide,
        depthWrite: false
    });
    private readonly rod = new CylinderGeometry(1, 1, 1, 12);
    private readonly disc = new CircleGeometry(1, 64);
    private readonly marker = new SphereGeometry(1, 16, 12);
    private readonly turn = new Quaternion();

    constructor(private readonly scene: Scene) {
        this.root.name = 'symmetry-guides';
        this.root.visible = false;
        this.scene.add(this.root);
    }

    setColor(color: Color): void {
        this.axisMaterial.color.copy(color);
        this.planeMaterial.color.copy(color);
    }

    show(guides: SymmetryGuideSet): void {
        this.root.clear();

        const principalOrder = guides.axes.reduce((highest, axis) => Math.max(highest, axis.order), 0);

        for (const axis of guides.axes) {
            const thickness = axis.order === principalOrder ? PRINCIPAL_AXIS_THICKNESS : AXIS_THICKNESS;
            const rod = this.placed(new Mesh(this.rod, this.axisMaterial), guides.center, UP, axis.direction);

            rod.scale.set(thickness, 2 * AXIS_REACH * guides.radius, thickness);
            this.root.add(rod);
        }

        for (const normal of guides.planeNormals) {
            const plane = this.placed(new Mesh(this.disc, this.planeMaterial), guides.center, FACING, normal);

            plane.scale.setScalar(PLANE_REACH * guides.radius);
            this.root.add(plane);
        }

        if (guides.inversionCenter) {
            const marker = new Mesh(this.marker, this.axisMaterial);

            marker.position.copy(guides.center);
            marker.scale.setScalar(INVERSION_MARKER_RADIUS);
            this.root.add(marker);
        }

        this.root.visible = true;
    }

    hide(): void {
        this.root.visible = false;
        this.root.clear();
    }

    dispose(): void {
        this.scene.remove(this.root);
        this.root.clear();
        this.rod.dispose();
        this.disc.dispose();
        this.marker.dispose();
        this.axisMaterial.dispose();
        this.planeMaterial.dispose();
    }

    private placed<T extends Object3D>(object: T, center: Vector3, along: Vector3, direction: Vector3): T {
        object.position.copy(center);
        object.quaternion.copy(this.turn.setFromUnitVectors(along, direction.clone().normalize()));

        return object;
    }
}
