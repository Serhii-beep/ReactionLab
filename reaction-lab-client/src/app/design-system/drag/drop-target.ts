import { computed, DestroyRef, Directive, ElementRef, inject, output } from "@angular/core";
import { DragSession } from "./drag-session";

@Directive({
    selector: '[rlDropTarget]',
    host: {
        '[attr.data-drop-over]': 'isOver() || null'
    }
})
export class DropTarget {
    readonly dropped = output<unknown>();

    protected readonly isOver = computed(() => this.session.isOver(this.id));

    private readonly session = inject(DragSession);
    private readonly id: number;

    constructor() {
        const element = inject<ElementRef<HTMLElement>>(ElementRef).nativeElement;

        this.id = this.session.register({ element, dropped: (payload) => this.dropped.emit(payload) });

        inject(DestroyRef).onDestroy(() => this.session.unregister(this.id));
    }
}