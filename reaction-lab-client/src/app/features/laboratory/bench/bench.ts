import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import * as icons from '../../../design-system/icons/icons.generated';
import { ChemFormula } from '../../../design-system/chemistry/chem-formula';
import { Chip } from '../../../design-system/primitives/chip/chip';
import { Icon } from '../../../design-system/icons/icon';
import { IconButton } from '../../../design-system/primitives/icon-button/icon-button';
import { TranslocoDirective } from '@jsverse/transloco';
import { stateSymbol } from '../state-symbol';
import { WorkspaceStore } from '../../../state/workspace-store';
import { SelectionStore } from '../../../state/selection-store';

@Component({
    selector: 'app-bench',
    templateUrl: './bench.html',
    styleUrl: './bench.scss',
    imports: [ChemFormula, Chip, Icon, IconButton, TranslocoDirective],
    changeDetection: ChangeDetectionStrategy.OnPush
})
export class Bench {
    protected readonly workspace = inject(WorkspaceStore);
    protected readonly selection = inject(SelectionStore);

    protected readonly icons = icons;
    protected readonly stateSymbol = stateSymbol;
}