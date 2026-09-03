import { computed, Service, signal } from "@angular/core";

export interface DragPoint {
    readonly x: number;
    readonly y: number;
}

interface DropZone {
    readonly element: HTMLElement;
    readonly dropped: (payload: unknown) => void;
}

@Service()
export class DragSession {
    private readonly zones = new Map<number, DropZone>();
    private readonly dragged = signal<unknown>(null);
    private readonly over = signal<number | null>(null);

    readonly payload = this.dragged.asReadonly();
    readonly position = signal<DragPoint | null>(null);
    readonly isDragging = computed(() => this.dragged() !== null);

    private sequence = 0;

    register(zone: DropZone): number {
        this.sequence += 1;
        this.zones.set(this.sequence, zone);

        return this.sequence;
    }

    unregister(id: number): void {
        this.zones.delete(id);
    }

    isOver(id: number): boolean {
        return this.over() === id;
    }

    start(payload: unknown, x: number, y: number): void {
        this.dragged.set(payload);
        this.moveTo(x, y);
    }

    moveTo(x: number, y: number): void {
        this.position.set({ x, y });
        this.over.set(this.zoneAt(x, y));
    }

    drop(): void {
        const id = this.over();
        const payload = this.dragged();

        if (id !== null && payload !== null) {
            this.zones.get(id)?.dropped(payload);
        }

        this.cancel();
    }

    cancel(): void {
        this.dragged.set(null);
        this.position.set(null);
        this.over.set(null);
    }

    private zoneAt(x: number, y: number): number | null {
        const target = document.elementFromPoint(x, y);

        if (target === null) {
            return null;
        }

        let match: number | null = null;

        for (const [id, zone] of this.zones) {
            if (zone.element.contains(target)) {
                return id;
            }
        }

        return null;
    }
}