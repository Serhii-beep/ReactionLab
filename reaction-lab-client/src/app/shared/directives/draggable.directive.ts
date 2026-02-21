import { Directive, ElementRef, inject, input, OnDestroy, OnInit, output } from "@angular/core";
import { DragDropService } from "../drag-drop/drag-drop.service";
import { DragItemType } from "../drag-drop/drag-drop.model";

@Directive({
    selector: '[appDraggable]',
    standalone: true,
    host: {
        '[class.dragging]': 'isDragging',
        '[style.cursor]': '"grab"'
    }
})
export class DraggableDirective<T = unknown> implements OnInit, OnDestroy {
    private readonly elementRef = inject(ElementRef<HTMLElement>);
    private readonly dragDropService = inject(DragDropService);

    dragType = input.required<DragItemType>({ alias: 'appDraggable' });
    dragData = input.required<T>();
    dragStarted = output<void>();
    dragEnded = output<void>();
    isDragging = false;

    private ghostElement: HTMLElement | null = null;
    private boundOnMouseMove!: (e: MouseEvent) => void;
    private boundOnMouseUp!: (e: MouseEvent) => void;
    private boundOnKeyDown!: (e: KeyboardEvent) => void;

    ngOnInit(): void {
        this.boundOnMouseMove = this.onMouseMove.bind(this);
        this.boundOnMouseUp = this.onMouseUp.bind(this);
        this.boundOnKeyDown = this.onKeyDown.bind(this);

        this.elementRef.nativeElement.addEventListener('mousedown', this.onMouseDown.bind(this));
    }

    ngOnDestroy(): void {
        this.cleanup();
        this.removeGhost();
    }

    private onMouseDown(event: MouseEvent): void {
        if (event.button !== 0) {
            return;
        }

        const target = event.target as HTMLElement;
        if (target.closest('button, a, input, [data-no-drag]')) {
            return;
        }

        event.preventDefault();

        this.isDragging = true;
        const position = { x: event.clientX, y: event.clientY };

        this.dragDropService.startDrag(this.dragType(), this.dragData(), position);
        this.createGhost(event);
        this.dragStarted.emit();

        document.addEventListener('mousemove', this.boundOnMouseMove);
        document.addEventListener('mouseup', this.boundOnMouseUp);
        document.addEventListener('keydown', this.boundOnKeyDown);
    }

    private onMouseMove(event: MouseEvent): void {
        if (!this.isDragging) {
            return;
        }

        const position = { x: event.clientX, y: event.clientY };
        this.dragDropService.updatePosition(position);
        this.updateGhostPosition(event);
    }

    private onMouseUp(event: MouseEvent): void {
        if (!this.isDragging) {
            return;
        }

        this.isDragging = false;
        this.dragDropService.endDrag();
        this.dragEnded.emit();
        this.cleanup();
        this.removeGhost();
    }

    private onKeyDown(event: KeyboardEvent): void {
        if (event.key === 'Escape') {
            this.isDragging = false;
            this.dragDropService.cancelDrag();
            this.dragEnded.emit();
            this.cleanup();
            this.removeGhost();
        }
    }

    private createGhost(event: MouseEvent): void {
        const element = this.elementRef.nativeElement;
        const rect = element.getBoundingClientRect();

        this.ghostElement = element.cloneNode(true) as HTMLElement;
        this.ghostElement.style.cssText = `
            position: fixed;
            top: 0;
            left: 0;
            width: ${rect.width}px;
            height: ${rect.height}px;
            pointer-events: none;
            z-index: var(--z-modal, 400);
            opacity: 0.85;
            transform: translate(${event.clientX - rect.width / 2}px, ${event.clientY - rect.height / 2}px) scale(1.05);
            box-shadow: 0 8px 24px rgba(0, 0, 0, 0.4);
            transition: none;
        `;

        document.body.appendChild(this.ghostElement);
    }

    private updateGhostPosition(event: MouseEvent): void {
        if (!this.ghostElement) {
            return;
        }

        const rect = this.elementRef.nativeElement.getBoundingClientRect();
        this.ghostElement.style.transform = `translate(${event.clientX - rect.width / 2}px, ${event.clientY - rect.height / 2}px) scale(1.05)`;
    }

    private removeGhost(): void {
        if (this.ghostElement) {
            this.ghostElement.remove();
            this.ghostElement = null;
        }
    }

    private cleanup(): void {
        document.removeEventListener('mousemove', this.boundOnMouseMove);
        document.removeEventListener('mouseup', this.boundOnMouseUp);
        document.removeEventListener('keydown', this.boundOnKeyDown);
    }
}