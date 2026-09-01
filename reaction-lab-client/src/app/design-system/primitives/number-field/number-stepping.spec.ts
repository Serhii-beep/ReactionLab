import { clamp, decimalsOf, snap, stepBy } from './number-stepping';

describe('number stepping', () => {
    it('reads the decimal precision from the step', () => {
        expect(decimalsOf(1)).toBe(0);
        expect(decimalsOf(0.5)).toBe(1);
        expect(decimalsOf(0.01)).toBe(2);
    });

    it('does not accumulate floating-point error across many steps', () => {
        const bounds = { min: 0, step: 0.1 };
        let value = 0;

        for (let i = 0; i < 7; i++) {
            value = stepBy(value, 1, bounds);
        }

        expect(value).toBe(0.7);
    });

    it('clamps to max even when the range is not a whole number of steps', () => {
        expect(stepBy(4, 1, { min: 0, max: 5, step: 2 })).toBe(5);
    });

    it('clamps to min going down', () => {
        expect(stepBy(1, -5, { min: 0, max: 10, step: 1 })).toBe(0);
    });

    it('steps below zero when unbounded', () => {
        expect(stepBy(-3, 1, { step: 1 })).toBe(-2);
        expect(stepBy(0, -1, { step: 0.25 })).toBe(-0.25);
    });

    it('snaps an off-grid value to the nearest step', () => {
        expect(snap(3.7, { min: 0, step: 0.5 })).toBe(3.5);
        expect(snap(3.8, { min: 0, step: 0.5 })).toBe(4);
    });

    it('honours a non-zero anchor when snapping', () => {
        expect(snap(7, { min: 1, step: 2 })).toBe(7);
        expect(snap(5.8, { min: 1, step: 2 })).toBe(5);
        expect(snap(6.4, { min: 1, step: 2 })).toBe(7);
    });

    it('clamps independently of the step grid', () => {
        expect(clamp(-4, { min: 0, max: 10, step: 1 })).toBe(0);
        expect(clamp(40, { min: 0, max: 10, step: 1 })).toBe(10);
        expect(clamp(4, { step: 1 })).toBe(4);
    });
});