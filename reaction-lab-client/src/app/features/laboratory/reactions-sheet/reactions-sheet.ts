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
import { PredictedReactions } from '../prediction/predicted-reactions';
import { DecimalPipe } from '@angular/common';
import { IconButton } from '../../../design-system/primitives/icon-button/icon-button';
import { LabConditionsStore, STANDARD_KELVIN } from '../../../state/lab-conditions-store';
import { ConditionsAnalysis } from '../conditions/conditions-analysis';
import { ConditionsFix, ConditionsVerdict, UphillDrive } from '../conditions/conditions-verdict';
import { celsiusOf, formatPowerOfTen } from '../conditions/conditions-format';
import { RequestLocale } from '../../../data/i18n/request-locale';

type ConditionsNote = 'boils' | 'wontGo' | 'settles' | UphillDrive;

const SKELETON_ROWS = [1, 2, 3];

@Component({
    selector: 'app-reactions-sheet',
    templateUrl: './reactions-sheet.html',
    styleUrl: './reactions-sheet.scss',
    imports: [Badge, Button, ChemEquation, ChemFormula, DecimalPipe, Dialog, EmptyState, IconButton, Skeleton, TranslocoDirective, Icon, PredictedReactions],
    changeDetection: ChangeDetectionStrategy.OnPush
})
export class ReactionsSheet {
    protected readonly ui = inject(UiStore);
    protected readonly workspace = inject(WorkspaceStore);
    protected readonly store = inject(ReactionStore);
    protected readonly run = inject(ReactionRun);
    protected readonly lab = inject(LabConditionsStore);
    protected readonly analysis = inject(ConditionsAnalysis);

    private readonly details = inject(SubstanceDetailsClient);
    private readonly notifications = inject(NotificationService);
    private readonly transloco = inject(TranslocoService);
    private readonly locale = inject(RequestLocale);

    protected readonly icons = icons;
    protected readonly skeletonRows = SKELETON_ROWS;
    protected readonly stockingReactionId = signal<string | null>(null);
    protected readonly celsiusOf = celsiusOf;

    protected readonly rows = computed(() => {
        const verdicts = this.analysis.verdicts();
        const warmed = this.lab.kelvin() !== STANDARD_KELVIN;

        return this.store.scored().map((scored) => {
            const verdict = verdicts.get(scored.reaction.id) ?? null;

            return {
                reaction: scored.reaction,
                readiness: scored.readiness,
                reactants: equationTerms(scored.reaction, 'Reactant'),
                products: equationTerms(scored.reaction, 'Product'),
                verdict,
                note: noteOf(verdict),
                blocked: verdict === null || verdict.blocksRun,
                conversionPercent: (verdict?.conversion ?? 0) * 100,
                rateFactor: warmed ? this.rateFactorOf(verdict) : null
            };
        });
    });

    protected applyFix(fix: ConditionsFix): void {
        if (fix.kind === 'standard') {
            this.lab.reset();
        } else {
            this.lab.setKelvin(fix.kelvin);
        }
    }

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

    private rateFactorOf(verdict: ConditionsVerdict | null): string | null {
        return verdict === null || verdict.log10RateFactor === null ? null : formatPowerOfTen(verdict.log10RateFactor, this.locale.lang());
    }
}

function noteOf(verdict: ConditionsVerdict | null): ConditionsNote | null {
    if (verdict === null) {
        return null;
    }

    if (verdict.boils) {
        return 'boils';
    }

    if (verdict.drivenUphillBy !== null) {
        return verdict.drivenUphillBy;
    }

    if (verdict.lean === 'reactants') {
        return 'wontGo';
    }

    return verdict.lean === 'equilibrium' ? 'settles' : null;
}