import { CommonModule } from "@angular/common";
import { ChangeDetectionStrategy, Component, computed, input, output } from "@angular/core";
import { MatButtonModule } from "@angular/material/button";
import { MatIconModule } from "@angular/material/icon";
import { MatTooltipModule } from "@angular/material/tooltip";
import { ReactionSummary, ReactionType } from "../../../../core/models";

@Component({
    selector: 'app-reaction-card',
    standalone: true,
    imports: [
        CommonModule,
        MatIconModule,
        MatButtonModule,
        MatTooltipModule
    ],
    templateUrl: './reaction-card.component.html',
    styleUrls: ['./reaction-card.component.scss'],
    changeDetection: ChangeDetectionStrategy.OnPush
})
export class ReactionCardComponent {
    reaction = input.required<ReactionSummary>();
    selected = input(false);

    reactionSelected = output<ReactionSummary>();
    executeReaction = output<ReactionSummary>();

    readonly reactionTypeLabel = computed(() => {
        const labels: Record<ReactionType, string> = {
            [ReactionType.Synthesis]: 'Synthesis',
            [ReactionType.Decomposition]: 'Decomposition',
            [ReactionType.SingleReplacement]: 'Single Replacement',
            [ReactionType.DoubleReplacement]: 'Double Replacement',
            [ReactionType.Combustion]: 'Combustion',
            [ReactionType.AcidBase]: 'Acid-Base',
            [ReactionType.Oxidation]: 'Oxidation',
            [ReactionType.Reduction]: 'Reduction',
            [ReactionType.Precipitation]: 'Precipitation',
            [ReactionType.Neutralization]: 'Neutralization'
        };
        return labels[this.reaction().reactionType] ?? 'Unknown';
    });

    readonly reactionTypeIcon = computed(() => {
        const icons: Record<ReactionType, string> = {
            [ReactionType.Synthesis]: 'add_circle',
            [ReactionType.Decomposition]: 'call_split',
            [ReactionType.SingleReplacement]: 'swap_horiz',
            [ReactionType.DoubleReplacement]: 'swap_horizontal_circle',
            [ReactionType.Combustion]: 'local_fire_department',
            [ReactionType.AcidBase]: 'science',
            [ReactionType.Oxidation]: 'bolt',
            [ReactionType.Reduction]: 'remove_circle',
            [ReactionType.Precipitation]: 'water_drop',
            [ReactionType.Neutralization]: 'balance'
        };
        return icons[this.reaction().reactionType] ?? 'science';
    });

    readonly difficultyStars = computed(() => {
        return Array(this.reaction().difficultyLevel).fill(0);
    });

    onSelect(): void {
        this.reactionSelected.emit(this.reaction());
    }

    onExecute(event: Event): void {
        event.stopPropagation();
        this.executeReaction.emit(this.reaction());
    }
}