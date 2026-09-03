import { ChangeDetectionStrategy, Component, computed, inject } from '@angular/core';
import { ChemFormula } from '../../../design-system/chemistry/chem-formula';
import * as icons from '../../../design-system/icons/icons.generated';
import { Button } from '../../../design-system/primitives/button/button';
import { EmptyState } from '../../../design-system/primitives/empty-state/empty-state';
import { SearchField } from '../../../design-system/primitives/search-field/search-field';
import { Skeleton } from '../../../design-system/primitives/skeleton/skeleton';
import { TranslocoDirective } from '@jsverse/transloco';
import { SubstancesClient } from '../../../data/substances/substances-client';
import { resourceError } from '../../../data/errors/resource-error';
import { stateSymbol } from '../state-symbol';
import { WorkspaceStore } from '../../../state/workspace-store';
import { substanceDrag } from '../drag-payload';
import { Draggable } from "../../../design-system/drag/draggable";

const SKELETON_ROWS = [1, 2, 3, 4, 5, 6];

@Component({
    selector: 'app-substances-panel',
    templateUrl: './substances-panel.html',
    styleUrl: './substances-panel.scss',
    imports: [Button, ChemFormula, EmptyState, SearchField, Skeleton, TranslocoDirective, Draggable],
    changeDetection: ChangeDetectionStrategy.OnPush
})
export class SubstancesPanel {
    protected readonly substances = inject(SubstancesClient);
    protected readonly workspace = inject(WorkspaceStore);

    protected readonly icons = icons;
    protected readonly skeletonRows = SKELETON_ROWS;
    protected readonly stateSymbol = stateSymbol;

    protected readonly error = computed(() => resourceError(this.substances.page.error()));
    protected readonly firstPage = computed(() => this.substances.page.isLoading() && this.substances.items().length === 0);
    protected readonly rows = computed(() => this.substances.items().map((substance) => ({ substance, payload: substanceDrag(substance) })));
}