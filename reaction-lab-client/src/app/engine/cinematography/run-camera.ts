import { MathUtils, Spherical, Vector3 } from "three";
import { Disposable } from "../core/disposal-scope";
import { CameraKey, CameraPath } from "./camera-path";
import { CameraShake } from "./camera-shake";
import { EngineContext } from "../core/engine-context";
import { CameraController } from "../interaction/camera-controller";
import { PointerInput } from "../interaction/pointer-input";
import { ReactionScript } from "../animation/reaction-script";
import { CameraCues } from "./camera-cues";
import { distancePerRadius, framedSphereOf } from "../core/camera-framing";

interface RunShot {
    readonly path: CameraPath;
    readonly shake: CameraShake;
}

const CLOSE_MARGIN = 1.3;
const REACH_MARGIN = 1.1;
const TRANSITION_CLOSENESS = 0.86;
const REVEAL_WIDENING = 1.02;
const REVEAL_LIFT = 0.1;
const CLOSE_POLAR = { dip: 0.05, lowest: 0.95, highest: 1.15, transitionDip: 0.03 };
const AZIMUTH_TURN = { approach: 0.35, transitionState: 0.55, reveal: 0.66, settled: 0.25 };
const TARGET_SHARE_OF_SHAKE = 0.6;

export class RunCamera implements Disposable {
    private shot: RunShot | null = null;
    private followedSeconds = 0;

    private readonly target = new Vector3();
    private readonly position = new Vector3();
    private readonly framedView = new Spherical();
    private readonly shakeOffset = new Vector3();
    private readonly stopListening: () => void;

    constructor(
        private readonly context: EngineContext,
        private readonly camera: CameraController,
        private readonly pointer: PointerInput
    ) {
        this.stopListening = camera.onTakeover(() => this.handBackUnlessPressedInPlace());
    }

    get directing(): boolean {
        return this.shot !== null;
    }

    get nearestDistance(): number {
        return this.shot === null ? Infinity : this.shot.path.nearestFramedRadius * distancePerRadius(this.context.camera);
    }

    begin(script: ReactionScript, cues: CameraCues): void {
        this.camera.currentPose(this.position, this.target);

        const startView = new Spherical().setFromVector3(this.position.sub(this.target));
        const keys = runKeysOf(script, cues, this.target, startView, distancePerRadius(this.context.camera));

        this.shot = {
            path: new CameraPath(keys),
            shake: new CameraShake(script.phases.bondsForm.start, script.durationSeconds, cues.enthalpyKilojoulesPerMole)
        };
    }

    follow(seconds: number): void {
        if (this.shot === null) {
            return;
        }

        const { path, shake } = this.shot;

        this.followedSeconds = seconds;
        path.sampleAt(seconds, this.target, this.framedView);

        const cameraDistance = this.framedView.radius * distancePerRadius(this.context.camera);

        this.position.setFromSphericalCoords(cameraDistance, this.framedView.phi, this.framedView.theta).add(this.target);
        shake.offsetAt(seconds, cameraDistance, this.shakeOffset);
        this.position.add(this.shakeOffset);
        this.target.addScaledVector(this.shakeOffset, TARGET_SHARE_OF_SHAKE);
        this.camera.pose(this.position, this.target);
    }

    end(): void {
        this.shot = null;
    }

    dispose(): void {
        this.stopListening();
    }

    private handBackUnlessPressedInPlace(): void {
        if (this.pointer.pressedInPlace) {
            this.follow(this.followedSeconds);
        } else {
            this.end();
        }
    }
}

function runKeysOf(script: ReactionScript, cues: CameraCues, startTarget: Vector3, startView: Spherical, distancePerFramedRadius: number): CameraKey[] {
    const { approach, transitionState, bondsForm } = script.phases;
    const { center, radius } = cues.gathered;
    const revealTarget = center.clone().setY(center.y + REVEAL_LIFT);
    const settled = framedSphereOf(cues.finalBounds);
    const close = radius * CLOSE_MARGIN;
    const closePolar = MathUtils.clamp(startView.phi - CLOSE_POLAR.dip, CLOSE_POLAR.lowest, CLOSE_POLAR.highest);
    const azimuth = startView.theta;
    const framedAt = (seconds: number, target: Vector3, authored: number) => Math.max(authored, cues.reachAt(seconds, target) * REACH_MARGIN);

    return [
        {
            seconds: 0,
            target: startTarget.clone(),
            framedRadius: startView.radius / distancePerFramedRadius,
            azimuthRadians: azimuth,
            polarRadians: startView.phi
        },
        {
            seconds: approach.end,
            target: center,
            framedRadius: framedAt(approach.end, center, close),
            azimuthRadians: azimuth + AZIMUTH_TURN.approach,
            polarRadians: closePolar
        },
        {
            seconds: transitionState.end,
            target: center,
            framedRadius: framedAt(transitionState.end, center, close * TRANSITION_CLOSENESS),
            azimuthRadians: azimuth + AZIMUTH_TURN.transitionState,
            polarRadians: closePolar - CLOSE_POLAR.transitionDip
        },
        {
            seconds: bondsForm.end,
            target: revealTarget,
            framedRadius: framedAt(bondsForm.end, revealTarget, close * REVEAL_WIDENING),
            azimuthRadians: azimuth + AZIMUTH_TURN.reveal,
            polarRadians: closePolar
        },
        {
            seconds: script.durationSeconds,
            target: settled.center,
            framedRadius: settled.radius,
            azimuthRadians: azimuth + AZIMUTH_TURN.settled,
            polarRadians: startView.phi
        }
    ];
}