import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import * as icons from '../../../design-system/icons/icons.generated';
import { ChemFormula } from '../../../design-system/chemistry/chem-formula';
import { Chip } from '../../../design-system/primitives/chip/chip';
import { Icon } from '../../../design-system/icons/icon';
import { IconButton } from '../../../design-system/primitives/icon-button/icon-button';
import { TranslocoDirective } from '@jsverse/transloco';
import { BenchStore } from '../../../state/bench-store';
import { stateSymbol } from '../state-symbol';

@Component({
    selector: 'app-bench',
    templateUrl: './bench.html',
    styleUrl: './bench.scss',
    imports: [ChemFormula, Chip, Icon, IconButton, TranslocoDirective],
    changeDetection: ChangeDetectionStrategy.OnPush
})
export class Bench {
    protected readonly bench = inject(BenchStore);

    protected readonly icons = icons;
    protected readonly stateSymbol = stateSymbol;
}