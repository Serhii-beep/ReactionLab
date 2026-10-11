import { ChangeDetectionStrategy, Component, input } from "@angular/core";
import { ChemFormula, ChemicalState } from "./chem-formula";

export interface EquationTerm {
    readonly formula: string;
    readonly coefficient?: number;
    readonly charge?: number;
    readonly state?: ChemicalState;
}

@Component({
    selector: 'rl-chem-equation-side',
    templateUrl: './chem-equation-side.html',
    styleUrl: './chem-equation-side.scss',
    imports: [ChemFormula],
    changeDetection: ChangeDetectionStrategy.OnPush
})
export class ChemEquationSide {
    readonly terms = input.required<readonly EquationTerm[]>();
    readonly showStates = input(true);
}