import { Vector3 } from "three";
import { MolecularSystem, Molecule } from "./molecular-system";
import { RandomSource } from "../../core/seeded-random";

export const BOLTZMANN_KILOJOULES_PER_MOLE_KELVIN = 0.0083145;

// The temperature the products cool toward once they have landed
export const BENCH_TEMPERATURE_KELVIN = 400;

export interface BathSetting {
    readonly frictionPerSecond: number;
    readonly temperatureKelvin: number;
    readonly moleculesKeepingDrift: readonly Molecule[];
}

export class LangevinBath {
    private readonly drift: Float64Array;
    private readonly noise: Float64Array;
    private readonly moleculeDrift = new Vector3();
    private readonly moleculeNoise = new Vector3();

    constructor(private readonly system: MolecularSystem, private readonly standardNormal: RandomSource) {
        this.drift = new Float64Array(system.velocities.length);
        this.noise = new Float64Array(system.velocities.length);
    }

    thermalize(setting: BathSetting, stepSeconds: number): void {
        const keep = Math.exp(-setting.frictionPerSecond * stepSeconds);
        const { velocities } = this.system;

        this.drawNoise(setting.temperatureKelvin, Math.sqrt(1 - keep * keep));
        this.keepDrifts(setting.moleculesKeepingDrift);

        for (let slot = 0; slot < velocities.length; slot++) {
            velocities[slot] = this.drift[slot] + keep * (velocities[slot] - this.drift[slot]) + this.noise[slot];
        }
    }

    private drawNoise(temperatureKelvin: number, noiseShare: number): void {
        this.system.atoms.forEach((atom, index) => {
            const spread = Math.sqrt((BOLTZMANN_KILOJOULES_PER_MOLE_KELVIN * temperatureKelvin) / atom.mass) * noiseShare;

            for (let axis = 0; axis < 3; axis++) {
                this.noise[index * 3 + axis] = spread * this.standardNormal();
            }
        });
    }

    private keepDrifts(molecules: readonly Molecule[]): void {
        this.drift.fill(0);

        for (const { atomIndices, mass } of molecules) {
            this.system.massWeightedMeanOf(atomIndices, mass, this.system.velocities, this.moleculeDrift);
            this.system.massWeightedMeanOf(atomIndices, mass, this.noise, this.moleculeNoise);

            for (const index of atomIndices) {
                this.drift[index * 3] = this.moleculeDrift.x;
                this.drift[index * 3 + 1] = this.moleculeDrift.y;
                this.drift[index * 3 + 2] = this.moleculeDrift.z;
                this.noise[index * 3] -= this.moleculeNoise.x;
                this.noise[index * 3 + 1] -= this.moleculeNoise.y;
                this.noise[index * 3 + 2] -= this.moleculeNoise.z;
            }
        }
    }
}