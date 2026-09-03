import { ChangeDetectionStrategy, Component, computed, inject, signal } from '@angular/core';
import * as icons from '../../../design-system/icons/icons.generated';
import { Button } from '../../../design-system/primitives/button/button';
import { EmptyState } from '../../../design-system/primitives/empty-state/empty-state';
import { SearchField } from '../../../design-system/primitives/search-field/search-field';
import { Skeleton } from '../../../design-system/primitives/skeleton/skeleton';
import { TranslocoDirective } from '@jsverse/transloco';
import { ElementsClient } from '../../../data/elements/elements-client';
import { resourceError } from '../../../data/errors/resource-error';

const SKELETON_ROWS = [1, 2, 3, 4, 5, 6];

@Component({
    selector: 'app-elements-panel',
    templateUrl: './elements-panel.html',
    styleUrl: './elements-panel.scss',
    imports: [Button, EmptyState, SearchField, Skeleton, TranslocoDirective],
    changeDetection: ChangeDetectionStrategy.OnPush
})
export class ElementsPanel {
    protected readonly elements = inject(ElementsClient);

    protected readonly icons = icons;
    protected readonly skeletonRows = SKELETON_ROWS;
    protected readonly query = signal('');

    protected readonly error = computed(() => resourceError(this.elements.all.error()));

    protected readonly visible = computed(() => {
        const needle = this.query().trim().toLocaleLowerCase();
        const all = this.elements.all.value();

        if (needle === '') {
            return all;
        }

        return all.filter((element) =>
            element.symbol.toLocaleLowerCase().startsWith(needle)
            || element.name.toLocaleLowerCase().includes(needle));
    });
}