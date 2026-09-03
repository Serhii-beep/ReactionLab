import { DestroyRef, Directive, inject, input, output, signal } from "@angular/core";
import { DragSession } from "./drag-session";

const THRESHOLD = 4;

@Directive({
    selector: '[rlDraggable]',
    host: {
        class: 'rl-draggable',
        '[attr.data-dragging]': 'dragging() || null',
        '(pointerdown)': 'onPointerDown($event)',
        '(pointermove)': 'onPointerMove($event)',
        '(pointerup)': 'onPointerUp($event)',
        '(pointercancel)': 'onPointerCancel($event)',
        '(click)': 'onClick($event)'
    }
})
export class Draggable {
    readonly dragPayload = input.required<unknown>();

    readonly activated = output<void>();

    protected readonly dragging = signal(false);

    private readonly session = inject(DragSession);

    private origin: DOMPointReadOnly | null = null;
    private pointerId: number | null = null;
    private latest = { x: 0, y: 0 };
    private frame = 0;
    private wasDrag = false;

    constructor() {
        inject(DestroyRef).onDestroy(() => this.reset());
    }

    protected onPointerDown(event: PointerEvent): void {
        if (!event.isPrimary || event.button !== 0) {
            return;
        }

        this.origin = new DOMPointReadOnly(event.clientX, event.clientY);
        this.latest = { x: event.clientX, y: event.clientY };
        this.pointerId = event.pointerId;

        (event.currentTarget as HTMLElement).setPointerCapture(event.pointerId);
    }

    protected onPointerMove(event: PointerEvent): void {
        if (this.origin === null) {
            return;
        }

        this.latest = { x: event.clientX, y: event.clientY };

        if (this.dragging()) {
            this.schedule();

            return;
        }

        if (Math.hypot(event.clientX - this.origin.x, event.clientY - this.origin.y) >= THRESHOLD) {
            this.dragging.set(true);
            this.session.start(this.dragPayload(), this.latest.x, this.latest.y);
        }
    }

    protected onPointerUp(event: PointerEvent): void {
        this.wasDrag = this.dragging();

        if (this.wasDrag) {
            this.session.moveTo(event.clientX, event.clientY);
            this.session.drop();
        }

        this.reset(event);
    }

    protected onPointerCancel(event: PointerEvent): void {
        this.session.cancel();
        this.reset(event);
    }

    protected onClick(event: MouseEvent): void {
        if (event.detail === 0 || !this.wasDrag) {
            this.activated.emit();
        }

        this.wasDrag = false;
    }

    private schedule(): void {
        if (this.frame !== 0) {
            return;
        }

        this.frame = requestAnimationFrame(() => {
            this.frame = 0;
            this.session.moveTo(this.latest.x, this.latest.y);
        });
    }

    private reset(event?: PointerEvent): void {
        if (this.frame !== 0) {
            cancelAnimationFrame(this.frame);
            this.frame = 0;
        }

        if (this.dragging()) {
            this.session.cancel();
        }

        const element = event?.currentTarget;

        if (element instanceof HTMLElement && this.pointerId !== null && element.hasPointerCapture(this.pointerId)) {
            element.releasePointerCapture(this.pointerId);
        }

        this.origin = null;
        this.pointerId = null;
        this.dragging.set(false);
    }
}