import { ChangeDetectionStrategy, Component, computed, inject } from '@angular/core';
import * as icons from '../../design-system/icons/icons.generated';
import { DockGroup, DockPresentation } from '../../design-system/layout/dock-group';
import { DockPanel } from '../../design-system/layout/dock-panel';
import { EmptyState } from '../../design-system/primitives/empty-state/empty-state';
import { TranslocoDirective } from '@jsverse/transloco';
import { Breakpoints } from '../../core/layout/breakpoints';

@Component({
    selector: 'app-laboratory',
    templateUrl: './laboratory.html',
    styleUrl: './laboratory.scss',
    imports: [DockGroup, DockPanel, EmptyState, TranslocoDirective],
    changeDetection: ChangeDetectionStrategy.OnPush
})
export class Laboratory {
    protected readonly icons = icons;

    private readonly breakpoints = inject(Breakpoints);

    protected readonly presentation = computed<DockPresentation>(() => this.breakpoints.size() === 'tablet' ? 'sheets' : 'docks');
}