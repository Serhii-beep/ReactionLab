import { computed, Service, signal } from '@angular/core';

export const KELVIN_AT_ZERO_CELSIUS = 273.15;
export const STANDARD_KELVIN = 298.15;
export const STANDARD_KILOPASCALS = 101.325;
export const COLDEST_KELVIN = 273.15;
export const HOTTEST_KELVIN = 1773.15;
export const LOWEST_KILOPASCALS = 1.01325;
export const HIGHEST_KILOPASCALS = 101325;

@Service()
export class LabConditionsStore {
    private readonly temperature = signal(STANDARD_KELVIN);
    private readonly pressure = signal(STANDARD_KILOPASCALS);

    readonly kelvin = this.temperature.asReadonly();
    readonly kilopascals = this.pressure.asReadonly();
    readonly isStandard = computed(() => this.temperature() === STANDARD_KELVIN && this.pressure() === STANDARD_KILOPASCALS);
    readonly celsius = computed(() => Math.round(this.temperature() - KELVIN_AT_ZERO_CELSIUS));
    readonly atmospheres = computed(() => this.pressure() / STANDARD_KILOPASCALS);

    setKelvin(kelvin: number): void {
        this.temperature.set(Math.min(Math.max(kelvin, COLDEST_KELVIN), HOTTEST_KELVIN));
    }

    setKilopascals(kilopascals: number): void {
        this.pressure.set(Math.min(Math.max(kilopascals, LOWEST_KILOPASCALS), HIGHEST_KILOPASCALS));
    }

    reset(): void {
        this.temperature.set(STANDARD_KELVIN);
        this.pressure.set(STANDARD_KILOPASCALS);
    }
}
