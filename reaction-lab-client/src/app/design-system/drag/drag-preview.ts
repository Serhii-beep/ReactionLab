import { ChangeDetectionStrategy, Component, computed, effect, ElementRef, inject } from "@angular/core";
import { DragSession } from "./drag-session";

@Component({
    selector: 'rl-drag-preview',
    template: '<ng-content />',
    styleUrl: './drag-preview.scss',
    changeDetection: ChangeDetectionStrategy.OnPush,
    host: {
        popover: 'manual',
        'aria-hidden': 'true',
        '[style.translate]': 'translate()'
    }
})
export class DragPreview {
    private readonly session = inject(DragSession);
    private readonly host = inject<ElementRef<HTMLElement>>(ElementRef);

    protected readonly translate = computed(() => {
        const point = this.session.position();

        return point === null ? null : `${point.x}px ${point.y}px`;
    });

    private shown = false;

    constructor() {
        effect(() => this.reveal(this.session.isDragging()));
    }

    private reveal(visible: boolean): void {
        const element = this.host.nativeElement;

        if (visible === this.shown || typeof element.showPopover !== 'function') {
            return;
        }

        this.shown = visible;

        if (visible) {
            element.showPopover();
        } else {
            element.hidePopover();
        }
    }
}