import { ChangeDetectionStrategy, Component, computed, input } from "@angular/core";
import { formatCharge, formulaParts } from "./chem-formula-parts";

export type ChemicalState = 's' | 'l' | 'g' | 'aq';

@Component({
    selector: 'rl-chem-formula',
    templateUrl: './chem-formula.html',
    styleUrl: './chem-formula.scss',
    changeDetection: ChangeDetectionStrategy.OnPush,
    host: {
        class: 'rl-chem-formula'
    }
})
export class ChemFormula {
    readonly formula = input.required<string>();
    readonly charge = input(0);
    readonly state = input<ChemicalState>();

    protected readonly parts = computed(() => formulaParts(this.formula()));
    protected readonly chargeText = computed(() => formatCharge(this.charge()));
}