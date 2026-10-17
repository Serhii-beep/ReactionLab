import { computed, effect, inject, Injectable, untracked } from "@angular/core";
import { SubstanceDetailsClient } from "../../../data/substances/substance-details-client";
import { Phase } from "../../../engine/core/matter";
import { BenchAmbient, ROOM_AMBIENT } from "../../../engine/scene/bench-ambient";
import { BenchScene } from "../../../engine/scene/bench-scene";
import { LabConditionsStore } from "../../../state/lab-conditions-store";
import { MoleculeBuilder } from "../builder/molecule-builder";
import { LabStates } from "../conditions/lab-states";
import { phaseOfState } from "./bench-units";

@Injectable()
export class BenchConditions {
    private readonly scene = inject(BenchScene);
    private readonly lab = inject(LabConditionsStore);
    private readonly labStates = inject(LabStates);
    private readonly details = inject(SubstanceDetailsClient);
    private readonly builder = inject(MoleculeBuilder);

    private readonly phaseBySubstanceId = computed(() => {
        const overrides = this.labStates.overrides();

        return new Map<string, Phase>([...this.details.loaded().values()].map((detail) =>
            [detail.id, phaseOfState(overrides.get(detail.id) ?? detail.stateAtRoomTemperature)]));
    });

    constructor() {
        effect(() => {
            const ambient: BenchAmbient = this.builder.active()
                ? ROOM_AMBIENT
                : { kelvin: this.lab.kelvin(), phaseBySubstanceId: this.phaseBySubstanceId() };

            untracked(() => this.scene.setAmbient(ambient));
        });
    }
}
