import { ChangeDetectionStrategy, Component, computed, inject } from '@angular/core';
import * as icons from '../../../design-system/icons/icons.generated';
import { Badge } from '../../../design-system/primitives/badge/badge';
import { Button } from '../../../design-system/primitives/button/button';
import { ChemEquation } from '../../../design-system/chemistry/chem-equation';
import { ChemFormula } from '../../../design-system/chemistry/chem-formula';
import { Dialog } from '../../../design-system/dialog/dialog';
import { EmptyState } from '../../../design-system/primitives/empty-state/empty-state';
import { Skeleton } from '../../../design-system/primitives/skeleton/skeleton';
import { TranslocoDirective } from '@jsverse/transloco';
import { UiStore } from '../../../state/ui-store';
import { WorkspaceStore } from '../../../state/workspace-store';
import { ReactionStore } from '../../../state/reaction-store';
import { equationTerms } from '../equation-terms';

const SKELETON_ROWS = [1, 2, 3];

@Component({
    selector: 'app-reactions-sheet',
    templateUrl: './reactions-sheet.html',
    styleUrl: './reactions-sheet.scss',
    imports: [Badge, Button, ChemEquation, ChemFormula, Dialog, EmptyState, Skeleton, TranslocoDirective],
    changeDetection: ChangeDetectionStrategy.OnPush
})
export class ReactionsSheet {
    protected readonly ui = inject(UiStore);
    protected readonly workspace = inject(WorkspaceStore);
    protected readonly store = inject(ReactionStore);

    protected readonly icons = icons;
    protected readonly skeletonRows = SKELETON_ROWS;

    protected readonly rows = computed(() =>
        this.store.scored().map((scored) => ({
            reaction: scored.reaction,
            readiness: scored.readiness,
            reactants: equationTerms(scored.reaction, 'Reactant'),
            products: equationTerms(scored.reaction, 'Product')
        })));
}