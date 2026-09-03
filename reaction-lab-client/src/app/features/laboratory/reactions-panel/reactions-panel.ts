import { ChangeDetectionStrategy, Component, computed, effect, inject } from '@angular/core';
import { ParticipantRole, ReactionSummary } from '../../../data/reactions/reaction';
import { readiness, Readiness } from '../../../data/reactions/reaction-readiness';
import { ChemEquation, EquationTerm } from '../../../design-system/chemistry/chem-equation';
import * as icons from '../../../design-system/icons/icons.generated';
import { Badge } from '../../../design-system/primitives/badge/badge';
import { Button } from '../../../design-system/primitives/button/button';
import { ChemFormula } from '../../../design-system/chemistry/chem-formula';
import { EmptyState } from '../../../design-system/primitives/empty-state/empty-state';
import { Skeleton } from '../../../design-system/primitives/skeleton/skeleton';
import { TranslocoDirective } from '@jsverse/transloco';
import { BenchStore } from '../../../state/bench-store';
import { ReactionsClient } from '../../../data/reactions/reactions-client';
import { resourceError } from '../../../data/errors/resource-error';
import { stateSymbol } from '../state-symbol';

const SKELETON_ROWS = [1, 2, 3];

interface ScoredReaction {
    readonly reaction: ReactionSummary;
    readonly reactants: readonly EquationTerm[];
    readonly products: readonly EquationTerm[];
    readonly readiness: Readiness;
}

@Component({
    selector: 'app-reactions-panel',
    templateUrl: './reactions-panel.html',
    styleUrl: './reactions-panel.scss',
    imports: [Badge, Button, ChemEquation, ChemFormula, EmptyState, Skeleton, TranslocoDirective],
    changeDetection: ChangeDetectionStrategy.OnPush
})
export class ReactionsPanel {
    protected readonly bench = inject(BenchStore);
    protected readonly reactions = inject(ReactionsClient);

    protected readonly icons = icons;
    protected readonly skeletonRows = SKELETON_ROWS;

    protected readonly error = computed(() => resourceError(this.reactions.page.error()));

    protected readonly scored = computed<readonly ScoredReaction[]>(() => {
        const counts = this.bench.counts();

        return this.reactions.reactions()
            .map((reaction) => ({
                reaction,
                reactants: terms(reaction, 'Reactant'),
                products: terms(reaction, 'Product'),
                readiness: readiness(reaction, counts)
            }))
            .sort((first, second) => Number(second.readiness.runnable) - Number(first.readiness.runnable));
    });

    constructor() {
        effect(() => this.reactions.available.set(this.bench.substanceIds()));
    }
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