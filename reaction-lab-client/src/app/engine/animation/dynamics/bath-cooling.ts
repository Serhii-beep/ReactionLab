import { smoothProgressBetween, smoothProgressIntegralBetween } from "../easing";
import { DynamicsSchedule } from "./dynamics-schedule";

export const AIR_DRAG_PER_SECOND = 1.1;

const COOLING_FRICTION_PER_SECOND = 2.6;
const COOLING_AFTER_RELEASE = { startSeconds: 0.5, fullSeconds: 1.6 };

export function coolingAt(schedule: DynamicsSchedule, seconds: number): number {
    return smoothProgressBetween(schedule.releaseSeconds + COOLING_AFTER_RELEASE.startSeconds, schedule.releaseSeconds + COOLING_AFTER_RELEASE.fullSeconds, seconds);
}

export function airFrictionPerSecondAt(schedule: DynamicsSchedule, seconds: number): number {
    return AIR_DRAG_PER_SECOND + COOLING_FRICTION_PER_SECOND * coolingAt(schedule, seconds);
}

export function energyKeptAt(schedule: DynamicsSchedule, seconds: number): number {
    const dragSeconds = Math.max(seconds - schedule.releaseSeconds, 0);
    const coolingSeconds = smoothProgressIntegralBetween(schedule.releaseSeconds + COOLING_AFTER_RELEASE.startSeconds, schedule.releaseSeconds + COOLING_AFTER_RELEASE.fullSeconds, seconds);

    return Math.exp(-(AIR_DRAG_PER_SECOND * dragSeconds + COOLING_FRICTION_PER_SECOND * coolingSeconds));
}