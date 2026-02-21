export type DragItemType = 'molecule' | 'element';

export interface DragData<T = unknown> {
    type: DragItemType;
    data: T;
}

export interface DragState<T = unknown> {
    isDragging: boolean;
    data: DragData<T> | null;
    startPosition: { x: number; y: number } | null;
    currentPosition: { x: number; y: number } | null;
}

export interface DropEvent<T = unknown> {
    data: DragData<T>;
    dropPosition: { x: number; y: number };
}