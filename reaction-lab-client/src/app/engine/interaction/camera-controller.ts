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
    private readonly cameraOffset = new Vector3();

    constructor(
        context: EngineContext,
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
        this.fenceTo(bounds, center);
        this.focus(center, distance, transition);
    }

    fenceTo(bounds: Box3, fallbackCenter: Vector3): void {
        if (bounds.isEmpty()) {
            this.fence.set(fallbackCenter, fallbackCenter);
        } else {
            this.fence.copy(bounds);
        }

        this.fence.expandByScalar(FENCE);
        this.fence.min.y = Math.max(this.fence.min.y, 0);
        this.controls.setBoundary(this.fence);
    }

    focus(center: Vector3, distance: number, transition: boolean): void {
        this.controls.setTarget(center.x, center.y, center.z, transition);
        this.controls.dollyTo(distance, transition);
    }

    pose(position: Vector3, target: Vector3): void {
        const offset = this.cameraOffset.subVectors(position, target);

        offset.setLength(MathUtils.clamp(offset.length(), MIN_DISTANCE, MAX_DISTANCE));
        this.controls.setLookAt(target.x + offset.x, target.y + offset.y, target.z + offset.z, target.x, target.y, target.z, false);
    }

    currentPose(position: Vector3, target: Vector3): void {
        this.controls.getPosition(position, false);
        this.controls.getTarget(target, false);
    }

    onTakeover(listener: () => void): () => void {
        this.controls.addEventListener('control', listener);
        this.controls.addEventListener('transitionstart', listener);

        return () => {
            this.controls.removeEventListener('control', listener);
            this.controls.removeEventListener('transitionstart', listener);
        };
    }

    update(delta: number): boolean {
        return this.controls.update(delta);
    }

    dispose(): void {
        this.controls.dispose();
    }
}