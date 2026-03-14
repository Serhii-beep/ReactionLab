import { CommonModule } from "@angular/common";
import { ChangeDetectionStrategy, Component, computed, inject } from "@angular/core";
import { MatIconModule } from "@angular/material/icon";
import { SelectionService } from "../../../../core/services/selection.service";
import { Atom3D } from "../../../../three-engine";
import { getCategoryConfig } from "../../../periodic-table";
import { MatterState } from "../../../../core/models";

@Component({
    selector: 'app-element-detail-panel',
    standalone: true,
    imports: [
        CommonModule,
        MatIconModule
    ],
    templateUrl: './element-detail-panel.component.html',
    styleUrls: ['./element-detail-panel.component.scss'],
    changeDetection: ChangeDetectionStrategy.OnPush
})
export class ElementDetailPanelComponent {
    private readonly selectionService = inject(SelectionService);

    readonly element = computed(() => {
        const data = this.selectionService.selectedData() as Atom3D | null;
        return data?.element ?? null;
    });

    readonly categoryConfig = computed(() => {
        const el = this.element();
        if (!el) {
            return;
        }

        return getCategoryConfig(el.category);
    });

    getStateLabel(state: MatterState): string {
        const labels: Record<MatterState, string> = {
            [MatterState.Solid]: 'Solid',
            [MatterState.Liquid]: 'Liquid',
            [MatterState.Gas]: 'Gas',
            [MatterState.Aqueous]: 'Aqueous',
            [MatterState.Plasma]: 'Plasma'
        };
        return labels[state] ?? 'Unknown';
    }

    getStateIcon(state: MatterState): string {
        const icons: Record<MatterState, string> = {
            [MatterState.Solid]: 'crop_square',
            [MatterState.Liquid]: 'water_drop',
            [MatterState.Gas]: 'cloud',
            [MatterState.Aqueous]: 'waves',
            [MatterState.Plasma]: 'bolt'
        };
        return icons[state] ?? 'science';
    }
}