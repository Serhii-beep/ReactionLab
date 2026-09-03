import { ChangeDetectionStrategy, Component, computed, inject } from '@angular/core';
import * as icons from '../../design-system/icons/icons.generated';
import { DockGroup, DockPresentation } from '../../design-system/layout/dock-group';
import { DockPanel } from '../../design-system/layout/dock-panel';
import { EmptyState } from '../../design-system/primitives/empty-state/empty-state';
import { TranslocoDirective } from '@jsverse/transloco';
import { Breakpoints } from '../../core/layout/breakpoints';
import { ElementsPanel } from "./elements-panel/elements-panel";
import { SubstancesPanel } from "./substances-panel/substances-panel";
import { Bench } from "./bench/bench";
import { ReactionsPanel } from "./reactions-panel/reactions-panel";

@Component({
    selector: 'app-laboratory',
    templateUrl: './laboratory.html',
    styleUrl: './laboratory.scss',
    imports: [DockGroup, DockPanel, EmptyState, TranslocoDirective, ElementsPanel, SubstancesPanel, Bench, ReactionsPanel],
    changeDetection: ChangeDetectionStrategy.OnPush
})
export class Laboratory {
    protected readonly icons = icons;

    private readonly breakpoints = inject(Breakpoints);

    protected readonly presentation = computed<DockPresentation>(() => this.breakpoints.size() === 'tablet' ? 'sheets' : 'docks');
}