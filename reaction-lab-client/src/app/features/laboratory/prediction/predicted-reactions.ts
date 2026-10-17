import { ChangeDetectionStrategy, Component, inject } from "@angular/core";
import { TranslocoDirective } from "@jsverse/transloco";
import * as icons from "../../../design-system/icons/icons.generated";
import { Icon } from "../../../design-system/icons/icon";
import { Badge } from "../../../design-system/primitives/badge/badge";
import { Spinner } from "../../../design-system/primitives/spinner/spinner";
import { ChemEquation } from "../../../design-system/chemistry/chem-equation";
import { BenchPrediction } from "./bench-prediction";
import { LabConditionsStore } from "../../../state/lab-conditions-store";

@Component({
    selector: 'app-predicted-reactions',
    templateUrl: './predicted-reactions.html',
    styleUrl: './predicted-reactions.scss',
    imports: [TranslocoDirective, Icon, Badge, Spinner, ChemEquation],
    changeDetection: ChangeDetectionStrategy.OnPush
})
export class PredictedReactions {
    protected readonly prediction = inject(BenchPrediction);
    protected readonly lab = inject(LabConditionsStore);
    protected readonly icons = icons;
}
