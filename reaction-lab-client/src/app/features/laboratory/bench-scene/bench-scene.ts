import { ChangeDetectionStrategy, Component, inject } from "@angular/core";
import { ChemFormula } from "../../../design-system/chemistry/chem-formula";
import { WorkspaceStore } from "../../../state/workspace-store";
import { SelectionStore } from "../../../state/selection-store";
import { stateSymbol } from "../state-symbol";

@Component({
    selector: 'app-bench-scene',
    templateUrl: './bench-scene.html',
    styleUrl: './bench-scene.scss',
    imports: [ChemFormula],
    changeDetection: ChangeDetectionStrategy.OnPush
})
export class BenchScene {
    protected readonly workspace = inject(WorkspaceStore);
    protected readonly selection = inject(SelectionStore);

    protected readonly stateSymbol = stateSymbol;
}