import { ChangeDetectionStrategy, Component, computed, inject, signal } from '@angular/core';
import * as icons from '../../../design-system/icons/icons.generated';
import { Badge } from '../../../design-system/primitives/badge/badge';
import { Button } from '../../../design-system/primitives/button/button';
import { ChemEquation } from '../../../design-system/chemistry/chem-equation';
import { ChemFormula } from '../../../design-system/chemistry/chem-formula';
import { Dialog } from '../../../design-system/dialog/dialog';
import { EmptyState } from '../../../design-system/primitives/empty-state/empty-state';
import { Skeleton } from '../../../design-system/primitives/skeleton/skeleton';
import { TranslocoDirective, TranslocoService } from '@jsverse/transloco';
import { UiStore } from '../../../state/ui-store';
import { WorkspaceStore } from '../../../state/workspace-store';
import { ReactionStore, ScoredReaction } from '../../../state/reaction-store';
import { equationTerms } from '../equation-terms';
import { Icon } from '../../../design-system/icons/icon';
import { ReactionRun } from '../run/reaction-run';
import { SubstanceDetailsClient } from '../../../data/substances/substance-details-client';
import { NotificationService } from '../../../core/notifications/notification-service';
import { finalize, tap } from 'rxjs';
import { missingPortions } from '../../../data/reactions/reaction-outcome';

const SKELETON_ROWS = [1, 2, 3];

@Component({
    selector: 'app-reactions-sheet',
    templateUrl: './reactions-sheet.html',
    styleUrl: './reactions-sheet.scss',
    imports: [Badge, Button, ChemEquation, ChemFormula, Dialog, EmptyState, Skeleton, TranslocoDirective, Icon],
    changeDetection: ChangeDetectionStrategy.OnPush
})
export class ReactionsSheet {
    protected readonly ui = inject(UiStore);
    protected readonly workspace = inject(WorkspaceStore);
    protected readonly store = inject(ReactionStore);
    protected readonly run = inject(ReactionRun);

    private readonly details = inject(SubstanceDetailsClient);
    private readonly notifications = inject(NotificationService);
    private readonly transloco = inject(TranslocoService);

    protected readonly icons = icons;
    protected readonly skeletonRows = SKELETON_ROWS;
    protected readonly stockingReactionId = signal<string | null>(null);

    protected readonly rows = computed(() =>
        this.store.scored().map((scored) => ({
            reaction: scored.reaction,
            readiness: scored.readiness,
            reactants: equationTerms(scored.reaction, 'Reactant'),
            products: equationTerms(scored.reaction, 'Product')
        })));

    protected addMissing(scored: ScoredReaction): void {
        const missing = scored.readiness.missing;

        this.stockingReactionId.set(scored.reaction.id);
        this.details.detailsOf(missing.map((reactant) => reactant.substanceId)).pipe(
            tap({
                next: (substances) => this.workspace.addPortions(missingPortions(missing, substances)),
                error: () => this.notifications.show('danger', this.transloco.translate('lab.reactions.stockFailed'))
            }),
            finalize(() => this.stockingReactionId.set(null))
        ).subscribe();
    }
}