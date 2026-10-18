import { computed, inject, Injectable } from "@angular/core";
import { ElementSummary } from "../../../data/elements/element";
import { ElementsClient } from "../../../data/elements/elements-client";
import { MatterState, SubstanceDetail, SubstanceSummary } from "../../../data/substances/substance";
import { SubstanceDetailsClient } from "../../../data/substances/substance-details-client";
import { LabConditionsStore, STANDARD_KILOPASCALS } from "../../../state/lab-conditions-store";
import { parseHillFormula } from "../scene/hill-formula";
import { waterBoilingKelvinAt } from "./water-boiling";

interface LabSetting {
    readonly kelvin: number;
    readonly kilopascals: number;
    readonly elements: ReadonlyMap<string, ElementSummary>;
}

const WATER = 'H2O';
const TROUTON_ENTROPY_OVER_GAS_CONSTANT = 10.5;
const CHANGEABLE_STATES: ReadonlySet<MatterState> = new Set<MatterState>(['Solid', 'Liquid', 'Gas']);

@Injectable()
export class LabStates {
    private readonly lab = inject(LabConditionsStore);
    private readonly details = inject(SubstanceDetailsClient);
    private readonly elements = inject(ElementsClient);

    readonly overrides = computed<ReadonlyMap<string, MatterState>>(() => {
        const setting: LabSetting = {
            kelvin: this.lab.kelvin(),
            kilopascals: this.lab.kilopascals(),
            elements: new Map(this.elements.table().map((element) => [element.symbol, element]))
        };
        const overrides = new Map<string, MatterState>();

        for (const detail of this.details.loaded().values()) {
            const state = labStateOf(detail, setting);

            if (state !== null && state !== detail.stateAtRoomTemperature) {
                overrides.set(detail.id, state);
            }
        }

        return overrides;
    }, { equal: sameStates });

    stateOf(substance: SubstanceSummary): MatterState | null {
        return this.overrides().get(substance.id) ?? substance.stateAtRoomTemperature;
    }
}

function labStateOf(detail: SubstanceDetail, setting: LabSetting): MatterState | null {
    if (detail.stateAtRoomTemperature === null || !CHANGEABLE_STATES.has(detail.stateAtRoomTemperature)) {
        return null;
    }

    if (detail.formula === WATER) {
        return setting.kelvin < waterBoilingKelvinAt(setting.kilopascals) ? 'Liquid' : 'Gas';
    }

    const parts = parseHillFormula(detail.hillFormula);

    return parts.length === 1 ? elementStateOf(setting.elements.get(parts[0].symbol), setting) : null;
}

function elementStateOf(element: ElementSummary | undefined, setting: LabSetting): MatterState | null {
    if (element === undefined || element.meltingPointKelvin === null || element.boilingPointKelvin === null) {
        return null;
    }

    if (setting.kelvin < element.meltingPointKelvin) {
        return 'Solid';
    }

    return setting.kelvin < boilingKelvinAt(element.boilingPointKelvin, setting.kilopascals) ? 'Liquid' : 'Gas';
}

function boilingKelvinAt(normalBoilingKelvin: number, kilopascals: number): number {
    return normalBoilingKelvin / (1 - Math.log(kilopascals / STANDARD_KILOPASCALS) / TROUTON_ENTROPY_OVER_GAS_CONSTANT);
}

function sameStates(first: ReadonlyMap<string, MatterState>, second: ReadonlyMap<string, MatterState>): boolean {
    return first.size === second.size && [...first].every(([substanceId, state]) => second.get(substanceId) === state);
}
