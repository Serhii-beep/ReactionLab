import { Disposable } from "../core/disposal-scope";

export interface PointerHandlers {
    readonly move: (x: number, y: number) => void;
    readonly leave: () => void;
    readonly click: (x: number, y: number) => void;
    readonly doubleClick: (x: number, y: number) => void;
}

export interface ClientPoint {
    readonly x: number;
    readonly y: number;
}

interface PointerPress extends ClientPoint {
    readonly id: number;
}

const DRAG_SLOP = 4;

export class PointerInput implements Disposable {
    private readonly document: Document;
    
    private handlers: PointerHandlers | null = null;
    private press: PointerPress | null = null;
    private dragged = false;
    private last: ClientPoint | null = null;

    constructor(
        private readonly element: HTMLElement
    ) {
        this.document = element.ownerDocument;
        element.addEventListener('pointermove', this.onMove);
        element.addEventListener('pointerleave', this.onLeave);
        element.addEventListener('pointerdown', this.onDown);
        element.addEventListener('click', this.onClick);
        element.addEventListener('dblclick', this.onDoubleClick);
        this.document.addEventListener('pointerup', this.onUp);
        this.document.addEventListener('pointercancel', this.onUp);
    }

    get lastPosition(): ClientPoint | null {
        return this.last;
    }

    bind(handlers: PointerHandlers): () => void {
        this.handlers = handlers;

        return () => {
            if (this.handlers === handlers) {
                this.handlers = null;
            }
        };
    }

    dispose(): void {
        this.element.removeEventListener('pointermove', this.onMove);
        this.element.removeEventListener('pointerleave', this.onLeave);
        this.element.removeEventListener('pointerdown', this.onDown);
        this.element.removeEventListener('click', this.onClick);
        this.element.removeEventListener('dblclick', this.onDoubleClick);
        this.document.removeEventListener('pointerup', this.onUp);
        this.document.removeEventListener('pointercancel', this.onUp);
        this.handlers = null;
    }

    private readonly onMove = (event: PointerEvent): void => {
        this.last = { x: event.clientX, y: event.clientY };

        if (this.press) {
            if (event.pointerId === this.press.id && !this.dragged) {
                this.dragged = Math.hypot(event.clientX - this.press.x, event.clientY - this.press.y) > DRAG_SLOP;
            }

            return;
        }

        this.handlers?.move(event.clientX, event.clientY);
    };

    private readonly onLeave = (): void => {
        this.last = null;
        this.handlers?.leave();
    };

    private readonly onDown = (event: PointerEvent): void => {
        if (this.press) {
            return;
        }

        this.press = { id: event.pointerId, x: event.clientX, y: event.clientY };
        this.dragged = false;
    };

    private readonly onUp = (event: PointerEvent): void => {
        if (this.press?.id === event.pointerId) {
            this.press = null;
        }
    };

    private readonly onClick = (event: MouseEvent): void => {
        if (!this.dragged && event.detail <= 1) {
            this.handlers?.click(event.clientX, event.clientY);
        }
    };

    private readonly onDoubleClick = (event: MouseEvent): void => {
        if (!this.dragged) {
            this.handlers?.doubleClick(event.clientX, event.clientY);
        }
    };
}