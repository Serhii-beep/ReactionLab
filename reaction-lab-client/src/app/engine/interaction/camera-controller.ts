import CameraControls from 'camera-controls';
import { Box3, MathUtils, Matrix4, Quaternion, Raycaster, Sphere, Spherical, Vector2, Vector3, Vector4 } from 'three';
import { Disposable } from '../core/disposal-scope';
import { EngineContext } from '../core/engine-context';

CameraControls.install({ THREE: { Vector2, Vector3, Vector4, Quaternion, Matrix4, Spherical, Box3, Sphere, Raycaster } });

const TRANSITION_TIME = 0.3;
const DRAG_TIME = 0.05;
const MIN_DISTANCE = 3;
const MAX_DISTANCE = 120;
const MIN_POLAR = MathUtils.degToRad(8);
const MAX_POLAR = MathUtils.degToRad(87);
const FENCE = 8;

export class CameraController implements Disposable {
    private readonly controls: CameraControls;
    private readonly fence = new Box3();

    constructor(
        private readonly context: EngineContext,
        element: HTMLElement
    ) {
        this.controls = new CameraControls(context.camera, element);
        this.controls.smoothTime = TRANSITION_TIME;
        this.controls.draggingSmoothTime = DRAG_TIME;
        this.controls.minDistance = MIN_DISTANCE;
        this.controls.maxDistance = MAX_DISTANCE;
        this.controls.minPolarAngle = MIN_POLAR;
        this.controls.maxPolarAngle = MAX_POLAR;
        this.controls.dollyToCursor = true;
        this.controls.boundaryFriction = 0.2;
        this.controls.mouseButtons.left = CameraControls.ACTION.ROTATE;
        this.controls.mouseButtons.middle = CameraControls.ACTION.DOLLY;
        this.controls.mouseButtons.right = CameraControls.ACTION.TRUCK;
        this.controls.mouseButtons.wheel = CameraControls.ACTION.DOLLY;
        this.controls.touches.one = CameraControls.ACTION.TOUCH_ROTATE;
        this.controls.touches.two = CameraControls.ACTION.TOUCH_DOLLY_TRUCK;
        this.controls.touches.three = CameraControls.ACTION.TOUCH_TRUCK;
    }

    get distance(): number {
        return this.controls.distance;
    }

    frame(center: Vector3, distance: number, bounds: Box3, transition: boolean): void {
        if (bounds.isEmpty()) {
            this.fence.set(center, center);
        } else {
            this.fence.copy(bounds);
        }

        this.fence.expandByScalar(FENCE);
        this.fence.min.y = Math.max(this.fence.min.y, 0);
        this.controls.setBoundary(this.fence);
        this.focus(center, distance, transition);
    }

    focus(center: Vector3, distance: number, transition: boolean): void {
        this.controls.setTarget(center.x, center.y, center.z, transition);
        this.controls.dollyTo(distance, transition);
    }

    update(delta: number): boolean {
        return this.controls.update(delta);
    }

    dispose(): void {
        this.controls.dispose();
    }
}