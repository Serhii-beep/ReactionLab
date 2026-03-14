import { CommonModule } from "@angular/common";
import { ChangeDetectionStrategy, Component, computed, inject } from "@angular/core";
import { MatIconModule } from "@angular/material/icon";
import { SelectionService } from "../../../../core/services/selection.service";
import { Molecule3D } from "../../../../three-engine";
import { MatterState } from "../../../../core/models";

@Component({
    selector: 'app-molecule-detail-panel',
    standalone: true,
    imports: [
        CommonModule,
        MatIconModule
    ],
    templateUrl: './molecule-detail-panel.component.html',
    styleUrls: ['./molecule-detail-panel.component.scss'],
    changeDetection: ChangeDetectionStrategy.OnPush
})
export class MoleculeDetailPanelComponent {
    private readonly selectionService = inject(SelectionService);

    readonly molecule = computed(() => {
        const data = this.selectionService.selectedData() as Molecule3D | null;
        return data?.molecule ?? null;
    });

    readonly atomCount = computed(() => {
        const data = this.selectionService.selectedData() as Molecule3D | null;
        return data?.atoms.length ?? 0;
    });

    readonly bondCount = computed(() => {
        const data = this.selectionService.selectedData() as Molecule3D | null;
        return data?.bonds.length ?? 0;
    });

    getStateLabel(state: MatterState): string {
        return state.toString();
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