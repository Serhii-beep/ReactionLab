import { CommonModule } from "@angular/common";
import { ChangeDetectionStrategy, Component, inject, output, signal } from "@angular/core";
import { FormsModule } from "@angular/forms";
import { MatButtonModule } from "@angular/material/button";
import { MatFormFieldModule } from "@angular/material/form-field";
import { MatIconModule } from "@angular/material/icon";
import { MatInputModule } from "@angular/material/input";
import { MatProgressSpinnerModule } from "@angular/material/progress-spinner";
import { MatTooltipModule } from "@angular/material/tooltip";
import { ReactionCardComponent } from "../reaction-card/reaction-card.component";
import { InfiniteScrollDirective } from "../../../../shared";
import { ReactionDetectorService } from "../../../../core/services";
import { ReactionSummary } from "../../../../core/models";

@Component({
    selector: 'app-reactions-panel',
    standalone: true,
    imports: [
        CommonModule,
        FormsModule,
        MatIconModule,
        MatButtonModule,
        MatInputModule,
        MatFormFieldModule,
        MatTooltipModule,
        MatProgressSpinnerModule,
        ReactionCardComponent,
        InfiniteScrollDirective
    ],
    templateUrl: './reactions-panel.component.html',
    styleUrls: ['./reactions-panel.component.scss'],
    changeDetection: ChangeDetectionStrategy.OnPush
})
export class ReactionsPanelComponent {
    private readonly reactionDetector = inject(ReactionDetectorService);

    readonly isCollapsed = signal(false);
    readonly selectedReaction = signal<ReactionSummary | null>(null);

    readonly reactions = this.reactionDetector.availableReactions;
    readonly loading = this.reactionDetector.loading;
    readonly hasMore = this.reactionDetector.hasMore;
    readonly hasReactants = this.reactionDetector.hasReactants;
    readonly searchTerm = this.reactionDetector.searchTerm;
    readonly reactionsCount = this.reactionDetector.availableReactionsCount;

    collapsed = output<boolean>();
    executeReaction = output<ReactionSummary>();

    onSearchChange(term: string): void {
        this.reactionDetector.search(term);
    }

    clearSearch(): void {
        this.reactionDetector.clearSearch();
    }

    loadMore(): void {
        this.reactionDetector.loadMore();
    }

    onReactionSelected(reaction: ReactionSummary): void {
        this.selectedReaction.set(this.selectedReaction()?.id === reaction.id ? null : reaction);
    }

    onExecuteReaction(reaction: ReactionSummary): void {
        this.executeReaction.emit(reaction);
    }

    toggleCollapse(): void {
        this.isCollapsed.update(v => !v);
        this.collapsed.emit(this.isCollapsed());
    }
}