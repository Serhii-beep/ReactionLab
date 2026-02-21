import { computed, Injectable, signal } from "@angular/core";
import { DragData, DragItemType, DragState } from "./drag-drop.model";

@Injectable({
    providedIn: 'root'
})
export class DragDropService {
    private readonly _state = signal<DragState>({
        isDragging: false,
        data: null,
        startPosition: null,
        currentPosition: null
    });

    readonly state = this._state.asReadonly();
    readonly isDragging = computed(() => this._state().isDragging);
    readonly dragData = computed(() => this._state().data);
    readonly currentPosition = computed(() => this._state().currentPosition);

    startDrag<T>(type: DragItemType, data: T, startPosition: { x: number, y: number }): void {
        this._state.set({
            isDragging: true,
            data: { type, data },
            startPosition,
            currentPosition: startPosition
        });

        document.body.style.cursor = 'grabbing';
        document.body.style.userSelect = 'none';
    }

    updatePosition(position: { x: number, y: number }): void {
        if (!this._state().isDragging) {
            return;
        }

        this._state.update(state => ({
            ...state,
            currentPosition: position
        }));
    }

    endDrag(): DragData | null {
        const data = this._state().data;

        this._state.set({
            isDragging: false,
            data: null,
            startPosition: null,
            currentPosition: null
        });

        document.body.style.cursor = '';
        document.body.style.userSelect = '';

        return data;
    }

    cancelDrag(): void {
        this.endDrag();
    }
}