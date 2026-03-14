import { CommonModule } from "@angular/common";
import { ChangeDetectionStrategy, Component, computed, inject, input, OnDestroy, OnInit, output, signal } from "@angular/core";
import { MatButtonModule } from "@angular/material/button";
import { MatIconModule } from "@angular/material/icon";
import { MatTooltipModule } from "@angular/material/tooltip";
import { SelectionService } from "../../../core/services/selection.service";
import { SceneManagerService } from "../../../three-engine";

export interface FloatingAction {
    id: string;
    icon: string;
    tooltip: string;
    color?: 'primary' | 'warn' | 'accent';
}

@Component({
    selector: 'app-floating-actions',
    standalone: true,
    imports: [
        CommonModule,
        MatIconModule,
        MatButtonModule,
        MatTooltipModule
    ],
    templateUrl: './floating-actions.component.html',
    styleUrls: ['./floating-actions.component.scss'],
    changeDetection: ChangeDetectionStrategy.OnPush
})
export class FloatingActionsComponent implements OnInit, OnDestroy {
    private readonly selectionService = inject(SelectionService);
    private readonly sceneManager = inject(SceneManagerService);

    actions = input<FloatingAction[]>([]);
    offsetY = input<number>(-60);

    actionClicked = output<string>();

    readonly isVisible = computed(() => this.selectionService.hasSelection());
    readonly screenPosition = signal<{ x: number; y: number } | null>(null);

    private animationFrameId: number | null = null;

    ngOnInit(): void {
        this.startPositionUpdates();
    }

    ngOnDestroy(): void {
        this.stopPositionUpdates();
    }

    private startPositionUpdates(): void {
        const updatePosition = () => {
            this.updateScreenPosition();
            this.animationFrameId = requestAnimationFrame(updatePosition);
        };
        this.animationFrameId = requestAnimationFrame(updatePosition);
    }

    private stopPositionUpdates(): void {
        if (this.animationFrameId !== null) {
            cancelAnimationFrame(this.animationFrameId);
            this.animationFrameId = null;
        }
    }

    private updateScreenPosition(): void {
        const objectId = this.selectionService.selectedObjectId();
        if (!objectId) {
            this.screenPosition.set(null);
            return;
        }

        const worldPos = this.sceneManager.getObjectBoundingBoxTop(objectId);
        if (!worldPos) {
            this.screenPosition.set(null);
            return;
        }

        const screenPos = this.sceneManager.worldToScreenPosition(worldPos);

        if (screenPos) {
            this.screenPosition.set({
                x: screenPos.x,
                y: screenPos.y + this.offsetY()
            });
        } else {
            this.screenPosition.set(null);
        }
    }

    onActionClick(actionId: string): void {
        this.actionClicked.emit(actionId);
    }
}