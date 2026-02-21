import { CommonModule } from "@angular/common";
import { ChangeDetectionStrategy, Component, computed, input, output } from "@angular/core";
import { MatIconModule } from "@angular/material/icon";
import { MatTooltipModule } from "@angular/material/tooltip";
import { MatterState, MoleculeSummary } from "../../../../core/models";
import { MatButtonModule } from "@angular/material/button";
import { DraggableDirective } from "../../../../shared";

@Component({
    selector: 'app-molecule-card',
    standalone: true,
    imports: [
        CommonModule,
        MatIconModule,
        MatTooltipModule,
        MatButtonModule,
        MatButtonModule,
        DraggableDirective
    ],
    templateUrl: './molecule-card.component.html',
    styleUrls: ['./molecule-card.component.scss'],
    changeDetection: ChangeDetectionStrategy.OnPush
})
export class MoleculeCardComponent {
    molecule = input.required<MoleculeSummary>();
    selected = input(false);

    moleculeSelected = output<MoleculeSummary>();
    addToScene = output<MoleculeSummary>();

    stateIcon = computed(() => {
        switch (this.molecule().stateAtRoomTemp) {
            case MatterState.Solid: return 'check_box_outline_blank';
            case MatterState.Liquid: return 'water_drop';
            case MatterState.Gas: return 'cloud';
            case MatterState.Aqueous: return 'waves';
            case MatterState.Plasma: return 'bolt';
            default: return 'science'
        }
    });

    stateLabel = computed(() => {
        return MatterState[this.molecule().stateAtRoomTemp] ?? 'Unknown';
    });

    onSelect(): void {
        this.moleculeSelected.emit(this.molecule());
    }

    onAddToScene(event: Event): void {
        event.stopPropagation();
        this.addToScene.emit(this.molecule());
    }
}