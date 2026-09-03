import { ChangeDetectionStrategy, Component, computed, inject } from '@angular/core';
import { ParticipantRole, ReactionSummary } from '../../../data/reactions/reaction';
import { ChemEquation, EquationTerm } from '../../../design-system/chemistry/chem-equation';
import * as icons from '../../../design-system/icons/icons.generated';
import { Badge } from '../../../design-system/primitives/badge/badge';
import { Button } from '../../../design-system/primitives/button/button';
import { ChemFormula } from '../../../design-system/chemistry/chem-formula';
import { EmptyState } from '../../../design-system/primitives/empty-state/empty-state';
import { Skeleton } from '../../../design-system/primitives/skeleton/skeleton';
import { TranslocoDirective } from '@jsverse/transloco';
import { stateSymbol } from '../state-symbol';
import { WorkspaceStore } from '../../../state/workspace-store';
import { ReactionStore } from '../../../state/reaction-store';

const SKELETON_ROWS = [1, 2, 3];

@Component({
    selector: 'app-reactions-panel',
    templateUrl: './reactions-panel.html',
    styleUrl: './reactions-panel.scss',
    imports: [Badge, Button, ChemEquation, ChemFormula, EmptyState, Skeleton, TranslocoDirective],
    changeDetection: ChangeDetectionStrategy.OnPush
})
export class ReactionsPanel {
    protected readonly workspace = inject(WorkspaceStore);
    protected readonly store = inject(ReactionStore);

    protected readonly icons = icons;
    protected readonly skeletonRows = SKELETON_ROWS;

    protected readonly rows = computed(() =>
        this.store.scored().map((scored) => ({
            reaction: scored.reaction,
            readiness: scored.readiness,
            reactants: terms(scored.reaction, 'Reactant'),
            products: terms(scored.reaction, 'Product')
        })));
}

function terms(reaction: ReactionSummary, role: ParticipantRole): readonly EquationTerm[] {
    return reaction.participants
        .filter((participant) => participant.role === role)
        .map((participant) => ({
            formula: participant.formula,
            coefficient: participant.coefficient,
            state: participant.state === null ? undefined : stateSymbol(participant.state)
        }));
}