import { ChangeDetectionStrategy, Component, computed, inject } from "@angular/core";
import { Dialog } from "../../../design-system/dialog/dialog";
import { ElementDetail } from "./element-detail";
import { ElementTile } from "../../../design-system/chemistry/element-tile";
import { PeriodicCell, PeriodicTableGrid } from "../../../design-system/chemistry/periodic-table-grid";
import { TranslocoDirective } from "@jsverse/transloco";
import { UiStore } from "../../../state/ui-store";
import { ElementsClient } from "../../../data/elements/elements-client";

const CATEGORIES = [
    'AlkaliMetal',
    'AlkalineEarthMetal',
    'TransitionMetal',
    'PostTransitionMetal',
    'Metalloid',
    'NonMetal',
    'Halogen',
    'NobleGas',
    'Lanthanide',
    'Actinide'
];

@Component({
    selector: 'app-periodic-table-sheet',
    templateUrl: './periodic-table-sheet.html',
    styleUrl: './periodic-table-sheet.scss',
    imports: [Dialog, ElementDetail, ElementTile, PeriodicTableGrid, TranslocoDirective],
    changeDetection: ChangeDetectionStrategy.OnPush
})
export class PeriodicTableSheet {
    protected readonly ui = inject(UiStore);

    private readonly elements = inject(ElementsClient);

    protected readonly categories = CATEGORIES;

    protected readonly cells = computed<readonly PeriodicCell[]>(() =>
        this.elements.all.value().map((element) => ({
            symbol: element.symbol,
            atomicNumber: element.atomicNumber,
            name: element.name,
            category: element.category,
            period: element.period,
            group: element.group
        })));

    protected readonly selected = computed(() => {
        const symbol = this.ui.tableElement();

        return this.elements.all.value().find((element) => element.symbol === symbol) ?? null;
    })
}