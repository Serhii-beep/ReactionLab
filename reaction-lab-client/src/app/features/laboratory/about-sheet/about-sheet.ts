import { ChangeDetectionStrategy, Component, computed, effect, inject, untracked } from '@angular/core';
import { ElementSummary } from '../../../data/elements/element';
import * as icons from '../../../design-system/icons/icons.generated';
import { DecimalPipe } from '@angular/common';
import { TranslocoDirective } from '@jsverse/transloco';
import { Button } from '../../../design-system/primitives/button/button';
import { ChemFormula } from '../../../design-system/chemistry/chem-formula';
import { Chip } from '../../../design-system/primitives/chip/chip';
import { Dialog } from '../../../design-system/dialog/dialog';
import { ElementTile } from '../../../design-system/chemistry/element-tile';
import { EmptyState } from '../../../design-system/primitives/empty-state/empty-state';
import { Icon } from '../../../design-system/icons/icon';
import { IconButton } from '../../../design-system/primitives/icon-button/icon-button';
import { Skeleton } from '../../../design-system/primitives/skeleton/skeleton';
import { UiStore } from '../../../state/ui-store';
import { SubstanceDetailsClient } from '../../../data/substances/substance-details-client';
import { ElementsClient } from '../../../data/elements/elements-client';
import { stateSymbol } from '../state-symbol';
import { readableCategory } from './readable-category';
import { SubstanceDetail } from '../../../data/substances/substance';
import { parseHillFormula } from '../scene/hill-formula';

export interface ConstituentElement {
    readonly element: ElementSummary;
    readonly atoms: number;
}

const SKELETON_ROWS = [1, 2, 3, 4];
const NAME_SEPARATOR = ' · ';

@Component({
    selector: 'app-about-sheet',
    templateUrl: './about-sheet.html',
    styleUrl: './about-sheet.scss',
    imports: [
        DecimalPipe,
        TranslocoDirective,
        Button,
        ChemFormula,
        Chip,
        Dialog,
        ElementTile,
        EmptyState,
        Icon,
        IconButton,
        Skeleton
    ],
    changeDetection: ChangeDetectionStrategy.OnPush
})
export class AboutSheet {
    protected readonly ui = inject(UiStore);

    private readonly details = inject(SubstanceDetailsClient);
    private readonly elements = inject(ElementsClient);

    protected readonly icons = icons;
    protected readonly stateSymbol = stateSymbol;
    protected readonly readableCategory = readableCategory;
    protected readonly skeletonRows = SKELETON_ROWS;

    protected readonly detail = computed<SubstanceDetail | null>(() => {
        const substanceId = this.ui.aboutSubstanceId();

        return substanceId === null ? null : this.details.loaded().get(substanceId) ?? null;
    });

    protected readonly name = computed(() => this.detail()?.name ?? null);

    protected readonly failed = computed(() => {
        const substanceId = this.ui.aboutSubstanceId();

        return substanceId !== null && this.details.failed().has(substanceId);
    });

    protected readonly constituents = computed<readonly ConstituentElement[]>(() => {
        const detail = this.detail();

        if (detail === null) {
            return [];
        }

        const elementsBySymbol = new Map(this.elements.all.value().map((element) => [element.symbol, element]));

        return parseHillFormula(detail.hillFormula).flatMap(({ symbol, count }) => {
            const element = elementsBySymbol.get(symbol);

            return element ? [{ element, atoms: count }] : [];
        });
    });

    protected readonly otherNames = computed(() => this.detail()?.commonNames.join(NAME_SEPARATOR) ?? '');

    protected readonly hasCuratedText = computed(() => {
        const detail = this.detail();

        return detail !== null && (
            detail.description !== null
            || detail.safetyInformation !== null
            || detail.commonNames.length > 0
            || detail.uses.length > 0
            || detail.interestingFacts.length > 0
        );
    });

    constructor() {
        effect(() => {
            const substanceId = this.ui.aboutSubstanceId();

            if (substanceId !== null) {
                untracked(() => this.details.ensure(substanceId));
            }
        });
    }

    protected retry(): void {
        const substanceId = this.ui.aboutSubstanceId();

        if (substanceId !== null) {
            this.details.retry(substanceId);
        }
    }
}