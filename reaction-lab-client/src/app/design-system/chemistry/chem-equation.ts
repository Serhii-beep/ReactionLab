import { ChangeDetectionStrategy, Component, computed, input } from "@angular/core";
import { ChemFormula, ChemicalState } from "./chem-formula";

export interface EquationTerm {
    readonly formula: string;
    readonly coefficient?: number;
    readonly charge?: number;
    readonly state?: ChemicalState;
}

const ARROW = '\u2192';
const EQUILIBRUM = '\u21CC';

@Component({
    selector: 'rl-chem-equation',
    templateUrl: './chem-equation.html',
    styleUrl: './chem-equation.scss',
    imports: [ChemFormula],
    changeDetection: ChangeDetectionStrategy.OnPush,
    host: {
        class: 'rl-chem-equation'
    }
})
export class ChemEquation {
    readonly reactants = input.required<readonly EquationTerm[]>();
    readonly products = input.required<readonly EquationTerm[]>();
    readonly reversible = input(false);
    readonly arrowLabel = input('yields');

    protected readonly sides = computed(() => [this.reactants(), this.products()]);
    protected readonly arrow = computed(() => (this.reversible() ? EQUILIBRUM : ARROW));
}