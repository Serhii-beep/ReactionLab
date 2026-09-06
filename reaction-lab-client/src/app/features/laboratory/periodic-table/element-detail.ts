import * as icons from '../../../design-system/icons/icons.generated';
import { ChangeDetectionStrategy, Component, computed, inject, input } from "@angular/core";
import { Button } from "../../../design-system/primitives/button/button";
import { ChemFormula } from "../../../design-system/chemistry/chem-formula";
import { DecimalPipe } from "@angular/common";
import { Draggable } from "../../../design-system/drag/draggable";
import { ElementTile } from "../../../design-system/chemistry/element-tile";
import { Icon } from "../../../design-system/icons/icon";
import { Skeleton } from "../../../design-system/primitives/skeleton/skeleton";
import { TranslocoDirective } from "@jsverse/transloco";
import { SubstancesClient } from "../../../data/substances/substances-client";
import { ElementSummary } from "../../../data/elements/element";
import { WorkspaceStore } from "../../../state/workspace-store";
import { stateSymbol } from '../state-symbol';
import { substanceDrag } from '../drag-payload';

const SKELETON_ROWS = [1, 2, 3, 4];

@Component({
    selector: 'app-element-detail',
    templateUrl: './element-detail.html',
    styleUrl: './element-detail.scss',
    imports: [Button, ChemFormula, DecimalPipe, Draggable, ElementTile, Icon, Skeleton, TranslocoDirective],
    providers: [SubstancesClient],
    changeDetection: ChangeDetectionStrategy.OnPush
})
export class ElementDetail {
    readonly element = input.required<ElementSummary>();

    protected readonly substances = inject(SubstancesClient);
    protected readonly workspace = inject(WorkspaceStore);

    protected readonly icons = icons;
    protected readonly skeletonRows = SKELETON_ROWS;
    protected readonly stateSymbol = stateSymbol;

    protected readonly rows = computed(() =>
    this.substances.items().map((substance) => ({ substance, payload: substanceDrag(substance) })));

    constructor() {
        this.substances.bindElement(() => this.element().symbol);
    }
}